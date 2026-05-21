using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IceBreakerApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;
    private readonly IHostApplicationLifetime _appLifetime;

    public TestController(ILogger<TestController> logger, IHostApplicationLifetime appLifetime)
    {
        _logger = logger;
        _appLifetime = appLifetime;
    }

    /// <summary>
    /// Проверка работоспособности API
    /// </summary>
    [HttpGet("ping")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Ping()
    {
        return Ok(new
        {
            message = "pong",
            timestamp = DateTime.UtcNow,
            containerId = Environment.GetEnvironmentVariable("HOSTNAME") ?? "unknown",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        });
    }

    /// <summary>
    /// Тестовая ошибка (исключение обрабатывается middleware)
    /// </summary>
    [HttpGet("error")]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult ThrowError()
    {
        _logger.LogWarning("Тестовая ошибка вызвана через /api/test/error");
        throw new InvalidOperationException("Test exception - container should stay alive (handled by GlobalExceptionHandlerMiddleware)");
    }

    /// <summary>
    /// ПРИНУДИТЕЛЬНОЕ ЗАВЕРШЕНИЕ ПРИЛОЖЕНИЯ - Docker должен перезапустить контейнер
    /// </summary>
    [HttpPost("shutdown")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Shutdown()
    {
        _logger.LogCritical("🚨 ПРИНУДИТЕЛЬНОЕ ЗАВЕРШЕНИЕ ПРИЛОЖЕНИЯ! Docker должен перезапустить контейнер!");
        
        // Запускаем завершение в фоне
        _ = Task.Run(async () =>
        {
            await Task.Delay(500);
            Environment.Exit(1);
        });

        return Ok(new
        {
            message = "Application is shutting down... Docker will restart it!",
            timestamp = DateTime.UtcNow,
            containerId = Environment.GetEnvironmentVariable("HOSTNAME") ?? "unknown"
        });
    }
    [HttpGet("instance-info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult InstanceInfo()
    {
        // Получаем полный HOSTNAME (хеш контейнера)
        var hostname = Environment.GetEnvironmentVariable("HOSTNAME") ?? "unknown";
    
        // Берём первые 8 символов хеша для читаемости
        var shortId = hostname.Length >= 8 ? hostname.Substring(0, 8) : hostname;
        var instanceId = $"web-{shortId}";
    
        Response.Headers.Append("X-Instance-ID", instanceId);
        Response.Headers.Append("X-Container-Hostname", hostname);
    
        return Ok(new
        {
            instanceId = instanceId,
            containerHostname = hostname,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Мягкое завершение через IHostApplicationLifetime (graceful shutdown)
    /// </summary>
    [HttpPost("graceful-shutdown")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GracefulShutdown()
    {
        _logger.LogWarning("Graceful shutdown requested via /api/test/graceful-shutdown");
        
        _ = Task.Run(async () =>
        {
            await Task.Delay(500);
            _appLifetime.StopApplication();
        });

        return Ok(new
        {
            message = "Application is stopping gracefully...",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Проверка подключения к БД
    /// </summary>
    [HttpGet("db-check")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckDatabase([FromServices] Infrastructure.ApplicationDbContext dbContext)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync();
            var usersCount = canConnect ? await dbContext.Users.CountAsync() : 0;
            
            return Ok(new
            {
                databaseConnected = canConnect,
                database = dbContext.Database.GetDbConnection().Database,
                usersCount = usersCount,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                databaseConnected = false,
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Тестовая аутентификация (требует валидный JWT токен)
    /// </summary>
    [Authorize]
    [HttpGet("secure")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Secure()
    {
        var username = User.Identity?.Name ?? "unknown";
        var userId = User.FindFirst("nameid")?.Value ?? "unknown";
        
        return Ok(new
        {
            message = $"Hello, {username}! This is a secure endpoint.",
            userId = userId,
            claims = User.Claims.Select(c => new { c.Type, c.Value }),
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Тестовый эндпоинт для администраторов
    /// </summary>
    [Authorize(Policy = "RequireAdminRole")]
    [HttpGet("admin-only")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult AdminOnly()
    {
        return Ok(new
        {
            message = "Welcome, Admin!",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Информация о системе
    /// </summary>
    [HttpGet("system-info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult SystemInfo()
    {
        return Ok(new
        {
            machineName = Environment.MachineName,
            osVersion = Environment.OSVersion.ToString(),
            processorCount = Environment.ProcessorCount,
            clrVersion = Environment.Version.ToString(),
            workingSet = Environment.WorkingSet / 1024 / 1024 + " MB",
            timestamp = DateTime.UtcNow,
            containerId = Environment.GetEnvironmentVariable("HOSTNAME") ?? "unknown"
        });
    }

    /// <summary>
    /// Тестовый эндпоинт с задержкой
    /// </summary>
    [HttpGet("delay/{milliseconds:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Delay(int milliseconds)
    {
        if (milliseconds > 10000) milliseconds = 10000; // максимум 10 секунд
        
        _logger.LogInformation($"Delaying for {milliseconds}ms...");
        await Task.Delay(milliseconds);
        
        return Ok(new
        {
            message = $"Waited for {milliseconds}ms",
            timestamp = DateTime.UtcNow
        });
    }
}