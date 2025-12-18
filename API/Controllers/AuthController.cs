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
    /// Регистрация нового пользователя
    /// </summary>
    [HttpPost("register")]
    [SwaggerOperation(Summary = "Register new user")]
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
    /// Вход в систему
    /// </summary>
    [HttpPost("login")]
    [SwaggerOperation(Summary = "User login")]
    [SwaggerResponse(200, "Success", typeof(LoginResponseDTO))]
    [SwaggerResponse(400, "Invalid credentials")]
    public async Task<ActionResult<LoginResponseDTO>> Login(
        [FromBody] LoginDTO loginDto,
        CancellationToken cancellationToken = default)
    {
        var result = await _authService.LoginAsync(loginDto, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Обновление access token
    /// </summary>
    [HttpPost("refresh")]
    [SwaggerOperation(Summary = "Refresh access token")]
    [SwaggerResponse(200, "Success", typeof(LoginResponseDTO))]
    [SwaggerResponse(400, "Invalid tokens")]
    public async Task<ActionResult<LoginResponseDTO>> RefreshToken(
        [FromBody] RefreshTokenDTO refreshTokenDto,
        CancellationToken cancellationToken = default)
    {
        var result = await _authService.RefreshTokenAsync(refreshTokenDto, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Выход из системы (отзыв refresh token)
    /// </summary>
    [HttpPost("logout")]
    [SwaggerOperation(Summary = "User logout")]
    [SwaggerResponse(200, "Success", typeof(object))]
    public async Task<ActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var success = await _authService.LogoutAsync(request.RefreshToken, cancellationToken);
        return Ok(new { success, message = "Logged out successfully" });
    }

    /// <summary>
    /// Выход из всех сессий
    /// </summary>
    [HttpPost("logout-all")]
    [SwaggerOperation(Summary = "Logout from all sessions")]
    [SwaggerResponse(200, "Success", typeof(object))]
    public async Task<ActionResult> LogoutAll(
        [FromBody] LogoutAllRequest request,
        CancellationToken cancellationToken = default)
    {
        var success = await _authService.LogoutAllAsync(request.UserId, cancellationToken);
        return Ok(new { success, message = "All sessions revoked" });
    }

    /// <summary>
    /// Подтверждение email по токену
    /// </summary>
    [HttpPost("confirm-email")]
    [SwaggerOperation(Summary = "Confirm email address")]
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
    [HttpPost("resend-confirmation")]
    [SwaggerOperation(Summary = "Resend confirmation email")]
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
    [HttpPost("revoke")]
    [SwaggerOperation(Summary = "Revoke specific refresh token")]
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