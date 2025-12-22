using FluentAssertions;
using FluentValidation;
using IceBreakerApp.API.Controllers;
using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.DTOs.Response;
using IceBreakerApp.Application.IRepositories;
using IceBreakerApp.Application.IServices;
using IceBreakerApp.Application.Services;
using IceBreakerApp.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace IceBreakerApp.Tests;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IValidator<RegisterRequestDTO>> _registerValidatorMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _registerValidatorMock = new Mock<IValidator<RegisterRequestDTO>>();
        _controller = new AuthController(
            _authServiceMock.Object, 
            _registerValidatorMock.Object);
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new RegisterRequestDTO
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "ValidPass123!",
            FirstName = "Test",
            LastName = "User"
        };

        var validationResult = new FluentValidation.Results.ValidationResult();
        _registerValidatorMock.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var expectedResponse = new RegisterResponseDTO
        {
            Success = true,
            Message = "Registration successful",
            UserId = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com"
        };

        _authServiceMock.Setup(x => x.RegisterAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Should().BeOfType<ActionResult<RegisterResponseDTO>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<RegisterResponseDTO>().Subject;
        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Register_InvalidValidation_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequestDTO
        {
            Username = "testuser",
            Email = "invalid-email",
            Password = "123",
            FirstName = "Test",
            LastName = "User"
        };

        var validationResult = new FluentValidation.Results.ValidationResult();
        validationResult.Errors.Add(new FluentValidation.Results.ValidationFailure("Email", "Invalid email format"));
        
        _registerValidatorMock.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequest.Value.Should().BeOfType<RegisterResponseDTO>().Subject;
        response.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        // Arrange
        var loginDto = new LoginDTO
        {
            EmailOrUsername = "test@example.com",
            Password = "ValidPass123!"
        };

        var expectedResponse = new LoginResponseDTO
        {
            AccessToken = "fake-jwt-token",
            RefreshToken = "fake-refresh-token",
            User = new LoginResponseDTO.UserInfo
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                Email = "test@example.com"
            }
        };

        _authServiceMock.Setup(x => x.LoginAsync(loginDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        result.Should().BeOfType<ActionResult<LoginResponseDTO>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LoginResponseDTO>().Subject;
        response.AccessToken.Should().NotBeNullOrEmpty();
        response.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_InvalidCredentials_ThrowsException()
    {
        // Arrange
        var loginDto = new LoginDTO
        {
            EmailOrUsername = "test@example.com",
            Password = "WrongPassword"
        };

        _authServiceMock.Setup(x => x.LoginAsync(loginDto, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Invalid credentials"));

        // Act & Assert
        await FluentActions.Invoking(() => _controller.Login(loginDto))
            .Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task RefreshToken_ValidToken_ReturnsNewTokens()
    {
        // Arrange
        var refreshTokenDto = new RefreshTokenDTO
        {
            RefreshToken = "valid-refresh-token"
        };

        var expectedResponse = new LoginResponseDTO
        {
            AccessToken = "new-jwt-token",
            RefreshToken = "new-refresh-token"
        };

        _authServiceMock.Setup(x => x.RefreshTokenAsync(refreshTokenDto, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.RefreshToken(refreshTokenDto);

        // Assert
        result.Should().BeOfType<ActionResult<LoginResponseDTO>>();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<LoginResponseDTO>().Subject;
        response.AccessToken.Should().Be("new-jwt-token");
    }

    [Fact]
    public async Task Logout_ValidToken_ReturnsSuccess()
    {
        // Arrange
        var request = new LogoutRequest
        {
            RefreshToken = "valid-refresh-token"
        };

        _authServiceMock.Setup(x => x.LogoutAsync(request.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Logout(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<object>().Subject;
    }
}

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRoleService> _roleServiceMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _roleServiceMock = new Mock<IRoleService>();
        _jwtServiceMock = new Mock<IJwtService>();
        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        _service = new AuthService(
            _userRepositoryMock.Object,
            _roleServiceMock.Object,
            _jwtServiceMock.Object,
            _emailServiceMock.Object,
            Mock.Of<IConfiguration>(),
            _loggerMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_NewUser_ReturnsSuccess()
    {
        // Arrange
        var request = new RegisterRequestDTO
        {
            Username = "newuser",
            Email = "new@example.com",
            Password = "ValidPass123!",
            FirstName = "New",
            LastName = "User"
        };

        _userRepositoryMock.Setup(x => x.EmailExistsAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userRepositoryMock.Setup(x => x.UsernameExistsAsync(request.Username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _emailServiceMock.Setup(x => x.SendWelcomeEmailAsync(request.Email, request.Username, "", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.RegisterAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("successful");
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsFailure()
    {
        // Arrange
        var request = new RegisterRequestDTO
        {
            Username = "user",
            Email = "existing@example.com",
            Password = "ValidPass123!"
        };

        _userRepositoryMock.Setup(x => x.EmailExistsAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.RegisterAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task LoginAsync_ValidUser_ReturnsTokens()
    {
        // Arrange
        var loginDto = new LoginDTO
        {
            EmailOrUsername = "test@example.com",
            Password = "ValidPass123!"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("ValidPass123!"),
            IsActive = true
        };

        _userRepositoryMock.Setup(x => x.FindByEmailAsync(loginDto.EmailOrUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var tokensResponse = new LoginResponseDTO
        {
            AccessToken = "fake-jwt-token",
            RefreshToken = "fake-refresh-token"
        };

        _jwtServiceMock.Setup(x => x.GenerateTokensAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokensResponse);

        // Act
        var result = await _service.LoginAsync(loginDto);

        // Assert
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        _userRepositoryMock.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ThrowsException()
    {
        // Arrange
        var loginDto = new LoginDTO
        {
            EmailOrUsername = "test@example.com",
            Password = "WrongPassword"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("ValidPass123!"),
            IsActive = true
        };

        _userRepositoryMock.Setup(x => x.FindByEmailAsync(loginDto.EmailOrUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        await FluentActions.Invoking(() => _service.LoginAsync(loginDto))
            .Should().ThrowAsync<Exception>();
    }
}