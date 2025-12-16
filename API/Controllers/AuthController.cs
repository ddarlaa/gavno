using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.IServices;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace IceBreakerApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
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
        var result = await _authService.RegisterAsync(request, cancellationToken);
        return Ok(result);
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
}