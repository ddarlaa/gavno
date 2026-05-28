using System;
using FluentMigrator.Runner;
using FluentValidation;
using FluentValidation.AspNetCore;
using IceBreakerApp.API.Middleware;
using IceBreakerApp.Application.Authorization;
using IceBreakerApp.Application.Authorization.Handlers;
using IceBreakerApp.Application.Authorization.Requirements;
using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.DTOs.ListItem;
using IceBreakerApp.Application.DTOs.Response;
using IceBreakerApp.Application.DTOs.Update;
using IceBreakerApp.Application.IRepositories;
using Infrastructure.Repositories;
using IceBreakerApp.Application.Services;
using IceBreakerApp.Application.Validators;
using IceBreakerApp.Application.IServices;
using IceBreakerApp.Domain.Models;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using IceBreakerApp.Application;
using IceBreakerApp.Application.DTOs.Create;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// КОНФИГУРАЦИЯ СЕРВИСОВ
// =============================================================================
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Логирование
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Настройка контекста базы данных для Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    options.EnableSensitiveDataLogging();
});

// Fluent Migrator
builder.Services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddPostgres()
        .WithGlobalConnectionString(connectionString)
        .ScanIn(typeof(Migrations.InitialCreate).Assembly).For.All())
    .AddLogging(lb => lb.AddFluentMigratorConsole());

// JWT Configuration
var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettingsSection);

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        var secret = jwtSettings["SecretKey"] ?? "your-super-secret-key-at-least-32-characters-long";
        var issuer = jwtSettings["Issuer"] ?? "IceBreakerApp";
        var audience = jwtSettings["Audience"] ?? "IceBreakerAppUsers";

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,

            ValidateAudience = true,
            ValidAudience = audience,

            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ClockSkew = TimeSpan.Zero
        };
        options.TokenValidationParameters.NameClaimType = "nameid";
        
        // ВАЖНО: Настройка для SignalR
       options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var path = context.HttpContext.Request.Path;
                if (path.StartsWithSegments("/hubs/file-notifications"))
                {
                    var token = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(token))
                    {
                        context.Token = token;
                    }
                }
                return Task.CompletedTask;
            },
            
            // Обработка ошибок аутентификации
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                
                logger.LogWarning($"Authentication failed: {context.Exception.Message}");
                
                // Для SignalR соединений
                if (context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    
                    var errorResponse = JsonSerializer.Serialize(new
                    {
                        error = "Unauthorized",
                        message = "Authentication failed"
                    });
                    
                    return context.Response.WriteAsync(errorResponse);
                }
                
                return Task.CompletedTask;
            },
            
            // Когда токен отсутствует
            OnChallenge = context =>
            {
                if (context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.HandleResponse();
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    
                    var errorResponse = JsonSerializer.Serialize(new
                    {
                        error = "Unauthorized",
                        message = "Token is required"
                    });
                    
                    return context.Response.WriteAsync(errorResponse);
                }
                
                return Task.CompletedTask;
            }
        };
    });

// Настройка CORS для SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRPolicy", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()  
              .SetIsOriginAllowed(_ => true);
    });
    
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireAdminRole());
    options.AddPolicy("RequireModeratorOrAdmin", policy => policy.RequireModeratorOrAdmin());
    options.AddPolicy("RequireUserOrAdmin", policy => policy.RequireRole("User", "Admin"));
    options.AddPolicy("RequireEmailConfirmed", policy => policy.RequireEmailConfirmed());
    options.AddPolicy("RequirePremiumSubscription", policy => policy.RequirePremiumSubscription());
    options.AddPolicy("CanEditQuestion", policy => 
        policy.RequireRole("Admin", "Moderator")
              .AddRequirements(new ResourceOwnerRequirement("question")));
    options.AddPolicy("CanDeleteQuestion", policy => 
        policy.RequireRole("Admin", "Moderator")
              .AddRequirements(new ResourceOwnerRequirement("question")));
    options.AddPolicy("CanEditAnswer", policy => 
        policy.RequireRole("Admin", "Moderator")
              .AddRequirements(new ResourceOwnerRequirement("answer")));
    options.AddPolicy("CanDeleteAnswer", policy => 
        policy.RequireRole("Admin", "Moderator")
              .AddRequirements(new ResourceOwnerRequirement("answer")));
    options.AddPolicy("CanEditUser", policy => 
        policy.RequireRole("Admin")
              .AddRequirements(new ResourceOwnerRequirement("user")));
    options.AddPolicy("CanViewReports", policy => 
        policy.RequireRole("Admin", "Moderator"));
});

// Контроллеры
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.DictionaryKeyPolicy = null;
    });

// Настройка SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1 MB
}).AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.PropertyNamingPolicy = null;
});

// Swagger Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IceBreakerApp API",
        Version = "v1",
        Description = "API для лабораторной работы",
        Contact = new OpenApiContact
        {
            Name = "Разработчик",
            Email = "email@example.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // JWT конфигурация для Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.\r\n\r\n" +
                      "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\n" +
                      "Example: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Настройка Kestrel
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 524288000; // 500 МБ
});

// Валидация
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddScoped<IValidator<RegisterRequestDTO>, RegisterRequestSyncValidator>();
builder.Services.AddScoped<RegisterRequestSyncValidator>();
builder.Services.AddMemoryCache();

// AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    cfg.CreateMap<User, UserResponseDTO>().ReverseMap();
    cfg.CreateMap<User, UserListItemDTO>();
    cfg.CreateMap<Question, QuestionResponseDTO>();
    cfg.CreateMap<UpdateQuestionDTO, Question>();
    cfg.CreateMap<CreateQuestionDTO, Question>();
    cfg.CreateMap<Topic, TopicResponseDTO>();
    cfg.CreateMap<CreateTopicDTO, Topic>();
    cfg.CreateMap<Topic, TopicListItemDTO>();
    cfg.CreateMap<QuestionAnswer, QuestionAnswerResponseDTO>();
    cfg.CreateMap<CreateQuestionAnswerDTO, QuestionAnswer>();
    cfg.CreateMap<BaseEntity, BaseEntity>();
});

// Health Checks
builder.Services.AddHealthChecks();

// =============================================================================
// РЕГИСТРАЦИЯ РЕПОЗИТОРИЕВ И СЕРВИСОВ
// =============================================================================

// Репозитории
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserRoleRepository, UserRoleRepository>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<IQuestionAnswerRepository, QuestionAnswerRepository>();
builder.Services.AddScoped<IQuestionLikeRepository, QuestionLikeRepository>();
builder.Services.AddScoped<ITopicRepository, TopicRepository>();
builder.Services.AddScoped<IUploadSessionRepository, UploadSessionRepository>();
builder.Services.AddScoped<IFileMetadataRepository, FileMetadataRepository>();

// Сервисы
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITopicService, TopicService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IFileStorageSettings, StorageSettings>();
builder.Services.AddScoped<IChunkedFileService, ChunkedFileService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IQuestionAnswerService, QuestionAnswerService>();
builder.Services.AddScoped<IQuestionLikeService, QuestionLikeService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, MockEmailService>();
builder.Services.AddScoped<IAuthorizationHandler, ResourceOwnerRequirementHandler>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IFileNotificationService, FileNotificationService>();

// Настройка обработки Multipart форм
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = 524288000; // 500 МБ
    options.MemoryBufferThreshold = int.MaxValue;
});

// =============================================================================
// BUILD APP
// =============================================================================
var app = builder.Build();

// =============================================================================
// ПРИМЕНЕНИЕ FLUENT MIGRATOR МИГРАЦИЙ
// =============================================================================
try
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Запуск FluentMigrator миграций...");
    
    using var scope = app.Services.CreateScope();
    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

    var migrations = runner.MigrationLoader.LoadMigrations();
    
    if (migrations.Any())
    {
        logger.LogInformation($"Найдено {migrations.Count} миграций");
        runner.MigrateUp();
        logger.LogInformation("FluentMigrator миграции успешно применены");
    }
    else
    {
        logger.LogWarning("Миграции не найдены.");
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Ошибка при применении FluentMigrator миграций");
    // Не падаем, продолжаем работу
}

// =============================================================================
// КОНФИГУРАЦИЯ PIPELINE
// =============================================================================
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseCors("SignalRPolicy"); // ДОЛЖНО БЫТЬ ДО UseAuthentication и UseAuthorization
app.UseAuthentication(); 
app.UseAuthorization();

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "IceBreaker API v1");
    options.RoutePrefix = "swagger";
    options.DisplayRequestDuration();
    options.EnableFilter();
    options.EnableTryItOutByDefault();
});

// Настройка SignalR хаба
app.MapHub<FileNotificationHub>("/hubs/file-notifications")
   .RequireCors("SignalRPolicy");

app.MapControllers();
// Включаем сбор метрик
app.UseHttpMetrics(); // автоматические метрики HTTP
app.MapMetrics();     // эндпоинт /metrics

// Health Check

app.MapGet("/health", () => Results.StatusCode(500));
// =============================================================================
// ЗАПУСК ПРИЛОЖЕНИЯ
// =============================================================================
try
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Запуск Ice Breaker API...");
    logger.LogInformation($"Environment: {app.Environment.EnvironmentName}");
    logger.LogInformation($"SignalR Hub доступен по: /hubs/file-notifications");
    logger.LogInformation($"WebSocket URL: ws://localhost:5047/hubs/file-notifications");
    logger.LogInformation($"Swagger UI доступен по: {app.Urls.FirstOrDefault()}/swagger");
    
    app.Run();
}
catch (Exception ex)
{
    var logger = app.Services.GetService<ILogger<Program>>() ?? app.Logger;
    logger.LogCritical(ex, "Критическая ошибка при запуске приложения");
    throw;
}