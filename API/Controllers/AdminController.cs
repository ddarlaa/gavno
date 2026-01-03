using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.DTOs.ListItem;
using IceBreakerApp.Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using IAuthorizationService = IceBreakerApp.Application.Authorization.IAuthorizationService;

namespace IceBreakerApp.API.Controllers;

/// <summary>
/// Контроллер для администраторской панели
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IQuestionService _questionService;
    private readonly ITopicService _topicService;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IUserService userService,
        IQuestionService questionService,
        ITopicService topicService,
        IAuthorizationService authorizationService,
        ILogger<AdminController> logger)
    {
        _userService = userService;
        _questionService = questionService;
        _topicService = topicService;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    /// <summary>
    /// Получить статистику системы
    /// </summary>
    [HttpGet("statistics")]
    [Authorize(Policy = "CanViewReports")]
    [SwaggerOperation(Summary = "Get system statistics")]
    [SwaggerResponse(200, "Success")]
    public Task<ActionResult<object>> GetStatistics(
        CancellationToken cancellationToken = default)
    {
        // Демо-статистика
        var statistics = new
        {
            TotalUsers = 1250,
            TotalQuestions = 3456,
            TotalAnswers = 12340,
            TotalTopics = 45,
            ActiveUsersToday = 89,
            NewUsersThisMonth = 156,
            MostPopularTopic = "General Discussion",
            AverageQuestionsPerUser = 2.7
        };

        return Task.FromResult<ActionResult<object>>(Ok(statistics));
    }

    /// <summary>
    /// Получить список всех пользователей для админки
    /// </summary>
    [HttpGet("users")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "Get all users for admin panel")]
    [SwaggerResponse(200, "Success", typeof(PaginatedResult<UserListItemDTO>))]
    public async Task<ActionResult<PaginatedResult<UserListItemDTO>>> GetAllUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _userService.GetAllAsync(pageNumber, pageSize, search, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Назначить роль пользователю
    /// </summary>
    [HttpPost("users/{userId}/assign-role")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "Assign role to user")]
    [SwaggerResponse(200, "Success")]
    [SwaggerResponse(404, "User not found")]
    public Task<IActionResult> AssignRoleToUser(
        Guid userId,
        [FromBody] AssignRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        // Демо-реализация
        _logger.LogInformation("Assigning role {RoleName} to user {UserId}", request.RoleName, userId);
        
        return Task.FromResult<IActionResult>(Ok(new { 
            success = true, 
            message = $"Role '{request.RoleName}' assigned to user successfully" 
        }));
    }

    /// <summary>
    /// Удалить роль у пользователя
    /// </summary>
    [HttpDelete("users/{userId}/roles/{roleName}")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "Remove role from user")]
    [SwaggerResponse(200, "Success")]
    [SwaggerResponse(404, "User or role not found")]
    public Task<IActionResult> RemoveRoleFromUser(
        Guid userId,
        string roleName,
        CancellationToken cancellationToken = default)
    {
        // Демо-реализация
        _logger.LogInformation("Removing role {RoleName} from user {UserId}", roleName, userId);
        
        return Task.FromResult<IActionResult>(Ok(new { 
            success = true, 
            message = $"Role '{roleName}' removed from user successfully" 
        }));
    }

    /// <summary>
    /// Заблокировать/разблокировать пользователя
    /// </summary>
    [HttpPatch("users/{userId}/toggle-active")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "Toggle user active status")]
    [SwaggerResponse(200, "Success")]
    [SwaggerResponse(404, "User not found")]
    public Task<IActionResult> ToggleUserActiveStatus(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        // Демо-реализация
        _logger.LogInformation("Toggling active status for user {UserId}", userId);
        
        return Task.FromResult<IActionResult>(Ok(new { 
            success = true, 
            message = "User status updated successfully" 
        }));
    }

    /// <summary>
    /// Получить отчеты и жалобы
    /// </summary>
    [HttpGet("reports")]
    [Authorize(Policy = "CanViewReports")]
    [SwaggerOperation(Summary = "Get system reports")]
    [SwaggerResponse(200, "Success")]
    public Task<ActionResult<object>> GetReports(
        CancellationToken cancellationToken = default)
    {
        // Демо-данные отчетов
        var reports = new
        {
            TotalReports = 23,
            PendingReports = 8,
            ResolvedReports = 15,
            RecentReports = new[]
            {
                new { Id = Guid.NewGuid(), Type = "Spam", Description = "User posting spam content", Status = "Pending", CreatedAt = DateTime.UtcNow.AddDays(-1) },
                new { Id = Guid.NewGuid(), Type = "Inappropriate", Description = "Inappropriate language in answer", Status = "Under Review", CreatedAt = DateTime.UtcNow.AddDays(-2) }
            }
        };

        return Task.FromResult<ActionResult<object>>(Ok(reports));
    }

    /// <summary>
    /// Получить системные настройки
    /// </summary>
    [HttpGet("settings")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "Get system settings")]
    [SwaggerResponse(200, "Success")]
    public Task<ActionResult<object>> GetSystemSettings(
        CancellationToken cancellationToken = default)
    {
        var settings = new
        {
            RegistrationEnabled = true,
            EmailVerificationRequired = true,
            MaxQuestionsPerDay = 10,
            MaxAnswersPerDay = 20,
            DefaultUserRole = "User",
            SessionTimeoutMinutes = 30,
            PasswordMinLength = 8
        };

        return Task.FromResult<ActionResult<object>>(Ok(settings));
    }
}

/// <summary>
/// DTO для запроса назначения роли
/// </summary>
public class AssignRoleRequest
{
    public string RoleName { get; set; } = string.Empty;
}