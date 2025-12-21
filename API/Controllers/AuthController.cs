using FluentValidation;
using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.DTOs.Response;
using IceBreakerApp.Application.IServices;
using IceBreakerApp.Application.Validators;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace IceBreakerApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<RegisterRequestDTO> _registerValidator;

    public AuthController(IAuthService authService, IValidator<RegisterRequestDTO> registerValidator)
    {
        _authService = authService;
        _registerValidator = registerValidator;
    }

    /// <summary>
    /// Регистрация нового пользователя в системе
    /// </summary>
    /// <param name="request">Данные для регистрации: username, email, password, firstName, lastName</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Результат регистрации с сообщением и данными пользователя</returns>
    /// <example>
    /// POST /api/auth/register
    /// {
    ///   "username": "johndoe",
    ///   "email": "john@example.com",
    ///   "password": "SecurePassword123!",
    ///   "firstName": "John",
    ///   "lastName": "Doe"
    /// }
    /// </example>
    /// <response code="200">Пользователь успешно зарегистрирован</response>
    /// <response code="400">Ошибка валидации или пользователь с таким username/email уже существует</response>
    [HttpPost("register")]
    [SwaggerOperation(
        Summary = "Register new user",
        Description = "Создает новый аккаунт пользователя. Email подтверждение НЕ требуется - пользователь может сразу использовать систему."
    )]
    [SwaggerResponse(200, "Success", typeof(RegisterResponseDTO))]
    [SwaggerResponse(400, "Validation error or duplicate data")]
    public async Task<ActionResult<RegisterResponseDTO>> Register(
        [FromBody] RegisterRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        // Валидация через FluentValidation
        var validationResult = await _registerValidator.ValidateAsync(request, cancellationToken);
        
        if (!validationResult.IsValid)
        {
            return BadRequest(new RegisterResponseDTO
            {
                Success = false,
                Message = "Validation failed",
                Errors = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }).ToList<object>()
            });
        }

        var result = await _authService.RegisterAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Аутентификация пользователя и получение JWT токенов
    /// </summary>
    /// <param name="loginDto">Email или username и пароль</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Access и Refresh токены для авторизации</returns>
    /// <example>
    /// POST /api/auth/login
    /// {
    ///   "emailOrUsername": "john@example.com",
    ///   "password": "SecurePassword123!"
    /// }
    /// </example>
    /// <response code="200">Успешная аутентификация. Возвращает accessToken и refreshToken</response>
    /// <response code="400">Неверные учетные данные</response>
    /// <response code="401">Неверный email/username или пароль</response>
    [HttpPost("login")]
    [SwaggerOperation(
        Summary = "User login",
        Description = "Аутентифицирует пользователя и возвращает JWT токены для доступа к защищенным API endpoints."
    )]
    [SwaggerResponse(200, "Success", typeof(LoginResponseDTO))]
    [SwaggerResponse(400, "Invalid credentials")]
    [SwaggerResponse(401, "Unauthorized - invalid credentials")]
    public async Task<ActionResult<LoginResponseDTO>> Login(
        [FromBody] LoginDTO loginDto,
        CancellationToken cancellationToken = default)
    {
        var result = await _authService.LoginAsync(loginDto, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Обновление access token с использованием refresh token
    /// </summary>
    /// <param name="refreshTokenDto">Refresh token для получения нового access token</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Новые access и refresh токены</returns>
    /// <example>
    /// POST /api/auth/refresh
    /// {
    ///   "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    /// }
    /// </example>
    /// <response code="200">Токены успешно обновлены</response>
    /// <response code="400">Неверный или истекший refresh token</response>
    /// <response code="401">Unauthorized - invalid refresh token</response>
    [HttpPost("refresh")]
    [SwaggerOperation(
        Summary = "Refresh access token",
        Description = "Обновляет access token используя валидный refresh token. Используется когда access token истек."
    )]
    [SwaggerResponse(200, "Success", typeof(LoginResponseDTO))]
    [SwaggerResponse(400, "Invalid tokens")]
    [SwaggerResponse(401, "Unauthorized - invalid refresh token")]
    public async Task<ActionResult<LoginResponseDTO>> RefreshToken(
        [FromBody] RefreshTokenDTO refreshTokenDto,
        CancellationToken cancellationToken = default)
    {
        var result = await _authService.RefreshTokenAsync(refreshTokenDto, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Выход из системы с отзывом refresh token
    /// </summary>
    /// <param name="request">Refresh token для отзыва</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Результат операции выхода</returns>
    /// <example>
    /// POST /api/auth/logout
    /// {
    ///   "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    /// }
    /// </example>
    /// <response code="200">Успешный выход из системы</response>
    /// <response code="400">Неверный refresh token</response>
    [HttpPost("logout")]
    [SwaggerOperation(
        Summary = "User logout",
        Description = "Выход из системы с отзывом refresh token. Делает токен недействительным."
    )]
    [SwaggerResponse(200, "Success", typeof(object))]
    [SwaggerResponse(400, "Invalid refresh token")]
    public async Task<ActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var success = await _authService.LogoutAsync(request.RefreshToken, cancellationToken);
        return Ok(new { success, message = "Logged out successfully" });
    }

    /// <summary>
    /// Выход из всех активных сессий пользователя
    /// </summary>
    /// <param name="request">ID пользователя для отзыва всех сессий</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Результат отзыва всех сессий</returns>
    /// <example>
    /// POST /api/auth/logout-all
    /// {
    ///   "userId": "123e4567-e89b-12d3-a456-426614174000"
    /// }
    /// </example>
    /// <response code="200">Все сессии пользователя отозваны</response>
    /// <response code="400">Неверный userId</response>
    [HttpPost("logout-all")]
    [SwaggerOperation(
        Summary = "Logout from all sessions",
        Description = "Отзывает все активные refresh tokens пользователя. Используется при смене пароля или подозрении в компрометации аккаунта."
    )]
    [SwaggerResponse(200, "Success", typeof(object))]
    [SwaggerResponse(400, "Invalid user ID")]
    public async Task<ActionResult> LogoutAll(
        [FromBody] LogoutAllRequest request,
        CancellationToken cancellationToken = default)
    {
        var success = await _authService.LogoutAllAsync(request.UserId, cancellationToken);
        return Ok(new { success, message = "All sessions revoked" });
    }

    /// <summary>
    /// Подтверждение email адреса по токену
    /// </summary>
    /// <param name="token">Токен подтверждения email, полученный в письме</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Результат подтверждения email</returns>
    /// <example>
    /// POST /api/auth/confirm-email?token=abc123def456
    /// </example>
    /// <response code="200">Email успешно подтвержден</response>
    /// <response code="400">Недействительный или истекший токен подтверждения</response>
    [HttpPost("confirm-email")]
    [SwaggerOperation(
        Summary = "Confirm email address",
        Description = "Подтверждает email адрес пользователя. ВАЖНО: В текущей реализации двухфакторная валидация отключена - email считается подтвержденным автоматически при регистрации."
    )]
    [SwaggerResponse(200, "Success", typeof(object))]
    [SwaggerResponse(400, "Invalid or expired token")]
    public async Task<ActionResult> ConfirmEmail(
        [FromQuery] string token,
        CancellationToken cancellationToken = default)
    {
        var success = await _authService.ConfirmEmailAsync(token, cancellationToken);
        
        if (success)
        {
            return Ok(new { 
                success = true, 
                message = "Email successfully confirmed" 
            });
        }
        
        return BadRequest(new { 
            success = false, 
            message = "Invalid or expired confirmation token" 
        });
    }

    /// <summary>
    /// Повторная отправка письма с подтверждением email
    /// </summary>
    /// <param name="request">Email адрес для повторной отправки подтверждения</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Результат отправки письма подтверждения</returns>
    /// <example>
    /// POST /api/auth/resend-confirmation
    /// {
    ///   "email": "john@example.com"
    /// }
    /// </example>
    /// <response code="200">Письмо подтверждения отправлено</response>
    /// <response code="404">Пользователь не найден или email уже подтвержден</response>
    [HttpPost("resend-confirmation")]
    [SwaggerOperation(
        Summary = "Resend confirmation email",
        Description = "Повторно отправляет письмо с токеном подтверждения email. Используется если письмо не пришло или токен истек."
    )]
    [SwaggerResponse(200, "Success", typeof(object))]
    [SwaggerResponse(404, "User not found or email already confirmed")]
    public async Task<ActionResult> ResendConfirmationEmail(
        [FromBody] ResendConfirmationRequest request,
        CancellationToken cancellationToken = default)
    {
        var success = await _authService.ResendConfirmationEmailAsync(request.Email, cancellationToken);
        
        if (success)
        {
            return Ok(new { 
                success = true, 
                message = "Confirmation email sent" 
            });
        }
        
        return NotFound(new { 
            success = false, 
            message = "User not found or email already confirmed" 
        });
    }

    /// <summary>
    /// Отзыв конкретного refresh token
    /// </summary>
    /// <param name="request">Refresh token для отзыва</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Результат отзыва токена</returns>
    /// <example>
    /// POST /api/auth/revoke
    /// {
    ///   "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    /// }
    /// </example>
    /// <response code="200">Refresh token успешно отозван</response>
    /// <response code="400">Недействительный refresh token</response>
    [HttpPost("revoke")]
    [SwaggerOperation(
        Summary = "Revoke specific refresh token",
        Description = "Отзывает конкретный refresh token, делая его недействительным. Используется для управления активными сессиями."
    )]
    [SwaggerResponse(200, "Success", typeof(object))]
    [SwaggerResponse(400, "Invalid refresh token")]
    public async Task<ActionResult> RevokeRefreshToken(
        [FromBody] RevokeTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var success = await _authService.LogoutAsync(request.RefreshToken, cancellationToken);
        
        if (success)
        {
            return Ok(new { 
                success = true, 
                message = "Refresh token revoked successfully" 
            });
        }
        
        return BadRequest(new { 
            success = false, 
            message = "Invalid refresh token" 
        });
    }
}