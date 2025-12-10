using System.Security.Cryptography;
using System.Text;
using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.DTOs.Create;
using IceBreakerApp.Application.DTOs.Update;
using IceBreakerApp.Application.IServices;
using IceBreakerApp.Domain.Models;
using Microsoft.Extensions.Logging;
using AutoMapper;
using IceBreakerApp.Application.IRepositories;

namespace IceBreakerApp.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;
    private readonly IMapper _mapper;

    public UserService(
        IUserRepository userRepository, 
        ILogger<UserService> logger, 
        IMapper mapper)
    {
        _userRepository = userRepository;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<UserResponseDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        return user != null ? _mapper.Map<UserResponseDTO>(user) : null;
    }

    public async Task<PaginatedResult<UserResponseDTO>> GetAllAsync(
        int pageNumber, 
        int pageSize, 
        string? search = null, 
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _userRepository.GetPagedAsync(
            pageNumber, pageSize, search, cancellationToken);
        
        var userDTOs = _mapper.Map<List<UserResponseDTO>>(items);
        
        return new PaginatedResult<UserResponseDTO>(
            userDTOs, totalCount, pageNumber, pageSize);
    }

    public async Task<UserResponseDTO> CreateAsync(
        CreateUserDTO createDto, 
        CancellationToken cancellationToken = default)
    {
        // Проверка уникальности email
        var emailExists = await _userRepository.EmailExistsAsync(createDto.Email, cancellationToken);
        if (emailExists)
            throw new Exception($"Email '{createDto.Email}' already exists");
        
        // Проверка уникальности username
        var usernameExists = await _userRepository.UsernameExistsAsync(createDto.Username, cancellationToken);
        if (usernameExists)
            throw new Exception($"Username '{createDto.Username}' already exists");
        
        // Маппинг DTO в User
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = createDto.Username,
            Email = createDto.Email,
            PasswordHash = HashPassword(createDto.Password),
            DisplayName = createDto.DisplayName,
            Bio = createDto.Bio,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };
        
        // Сохранение
        await _userRepository.AddAsync(user, cancellationToken);
        
        _logger.LogInformation("User created: {UserId}, Username: {Username}", 
            user.Id, user.Username);
        
        // Маппинг обратно в DTO
        return _mapper.Map<UserResponseDTO>(user);
    }

    public async Task UpdateAsync(
        Guid id, 
        UpdateUserDTO updateDto, 
        CancellationToken cancellationToken = default)
    {
        // Получаем пользователя с отслеживанием изменений
        var user = await _userRepository.GetByIdWithTrackingAsync(id, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException($"User with ID {id} not found");
        
        // Обновление полей
        if (!string.IsNullOrWhiteSpace(updateDto.DisplayName))
            user.DisplayName = updateDto.DisplayName;
        
        if (!string.IsNullOrWhiteSpace(updateDto.Bio))
            user.Bio = updateDto.Bio;
        
        user.UpdatedAt = DateTime.UtcNow;
        
        await _userRepository.UpdateAsync(user, cancellationToken);
        
        _logger.LogInformation("User updated: {UserId}", id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var exists = await _userRepository.ExistsAsync(id, cancellationToken);
        if (!exists)
            throw new KeyNotFoundException($"User with ID {id} not found");
        
        await _userRepository.DeleteAsync(id, cancellationToken);
        
        _logger.LogInformation("User deleted: {UserId}", id);
    }

    public async Task<UserResponseDTO?> FindByEmailAsync(
        string email, 
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindByEmailAsync(email, cancellationToken);
        return user != null ? _mapper.Map<UserResponseDTO>(user) : null;
    }

    public async Task<UserResponseDTO?> FindByUsernameAsync(
        string username, 
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.FindByUsernameAsync(username, cancellationToken);
        return user != null ? _mapper.Map<UserResponseDTO>(user) : null;
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
    
    
}