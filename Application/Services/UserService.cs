using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.DTOs.Create;
using IceBreakerApp.Application.DTOs.Update;
using IceBreakerApp.Application.IServices;
using IceBreakerApp.Domain.Models;
using Microsoft.Extensions.Logging;
using AutoMapper;
using IceBreakerApp.Application.IRepositories;
using IceBreakerApp.Domain;

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
                Email = createDto.Email,
                Username = createDto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(createDto.Password),
                DisplayName = createDto.DisplayName,
                Bio = createDto.Bio,
                CreatedAt = DateTime.UtcNow.ToPostgreSafeUtc(),
                UpdatedAt = DateTime.UtcNow.ToPostgreSafeUtc(),
                IsEmailConfirmed = false,
                IsActive = true,
                IsDeleted = false
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
        
        user.UpdatedAt = DateTime.UtcNow.ToPostgreSafeUtc();
        
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

        public async Task<User?> AuthenticateUserAsync(string emailOrUsername, string password, CancellationToken cancellationToken = default)
        {
            // Поиск пользователя по email или username
            var user = await _userRepository.FindByEmailAsync(emailOrUsername, cancellationToken) ??
                       await _userRepository.FindByUsernameAsync(emailOrUsername, cancellationToken);

            if (user == null)
                return null;

            // Проверка активности и статуса
            if (!user.IsActive || user.IsDeleted)
                return null;

            // Проверка подтверждения email (опционально)
            if (!user.IsEmailConfirmed)
                return null;

            // Проверка пароля
            if (!VerifyPassword(password, user.PasswordHash))
                return null;

            return user;
        }

        public async Task UpdateLastLoginAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdWithTrackingAsync(userId, cancellationToken);
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow.ToPostgreSafeUtc();
                user.UpdatedAt = DateTime.UtcNow.ToPostgreSafeUtc();
                await _userRepository.UpdateAsync(user, cancellationToken);
            }
        }

        private static string HashPassword(string password)
        {
            // Использование BCrypt для хеширования пароля
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }

        private static bool VerifyPassword(string password, string passwordHash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, passwordHash);
            }
            catch
            {
                return false;
            }
        }
    
    
}