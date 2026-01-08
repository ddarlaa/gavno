using FluentMigrator.Runner;
using FluentMigrator.Runner.Processors;
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
using IceBreakerApp.Domain.IRepositories;
using IceBreakerApp.Domain.Models;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// КОНФИГУРАЦИЯ СЕРВИСОВ
// =============================================================================
// В самом начале метода Main
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Логирование
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();


// Настройка контекста базы данных для Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // EF Core используется только как ORM, миграции управляются FluentMigrator
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
var jwtSettings = jwtSettingsSection.Get<JwtSettings>();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var secretKey = jwtSettings?.SecretKey ?? throw new InvalidOperationException("JWT SecretKey is not configured");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    // Role-based policies
    options.AddPolicy("RequireAdminRole", policy => policy.RequireAdminRole());
    options.AddPolicy("RequireModeratorOrAdmin", policy => policy.RequireModeratorOrAdmin());
    options.AddPolicy("RequireUserOrAdmin", policy => policy.RequireRole("User", "Admin"));
    
    // Claim-based policies
    options.AddPolicy("RequireEmailConfirmed", policy => policy.RequireEmailConfirmed());
    options.AddPolicy("RequirePremiumSubscription", policy => policy.RequirePremiumSubscription());
    
    // Resource-based policies
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
builder.Services.AddControllers();

// Swagger Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IceBreakerApp API",
        Version = "v1",
        Description = "API для лабораторной работы по аутентификации"
    });

    // JWT поддержка в Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
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

// CORS
builder.Services.AddCors(options => options.AddPolicy("ApiCorsPolicy", policy =>
{
    policy.WithOrigins("http://localhost:3000", "http://localhost:8080", "https://yourdomain.com")
        .AllowAnyMethod()
        .AllowAnyHeader();
}));

// Валидация
builder.Services.AddFluentValidationAutoValidation();

// Явно регистрируем только синхронные валидаторы для избежания конфликтов с асинхронными
builder.Services.AddScoped<IValidator<RegisterRequestDTO>, RegisterRequestSyncValidator>();
builder.Services.AddScoped<RegisterRequestSyncValidator>();

// AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    // User маппинги
    cfg.CreateMap<User, UserResponseDTO>().ReverseMap();
    cfg.CreateMap<User, UserListItemDTO>();

    // Question маппинги
    cfg.CreateMap<Question, QuestionResponseDTO>();
    cfg.CreateMap<UpdateQuestionDTO, Question>();
    cfg.CreateMap<CreateQuestionDTO, Question>();

    // Topic маппинги
    cfg.CreateMap<Topic, TopicResponseDTO>();
    cfg.CreateMap<CreateTopicDTO, Topic>();
    cfg.CreateMap<Topic, TopicListItemDTO>();

    // QuestionAnswer маппинги
    cfg.CreateMap<QuestionAnswer, QuestionAnswerResponseDTO>();
    cfg.CreateMap<CreateQuestionAnswerDTO, QuestionAnswer>();

    // BaseEntity маппинг для наследников
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

// Репозитории для проверки владения ресурсами
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

// Сервисы вопросов и ответов
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IQuestionAnswerService, QuestionAnswerService>();
builder.Services.AddScoped<IQuestionLikeService, QuestionLikeService>();

// Сервисы авторизации
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, MockEmailService>();

// Authorization Services
builder.Services.AddScoped<IAuthorizationHandler, ResourceOwnerRequirementHandler>();
builder.Services.AddScoped<IRoleService, RoleService>();

var app = builder.Build();

// =============================================================================
// ПРИМЕНЕНИЕ FLUENTMIGRATOR МИГРАЦИЙ
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
        
        // Применяем миграции
        runner.MigrateUp();
        logger.LogInformation("FluentMigrator миграции успешно применены");
    }
    else
    {
        logger.LogWarning("Миграции не найдены. Создайте классы миграций.");
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

// Настройка HTTPS редиректа
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Middleware pipeline
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseCors("ApiCorsPolicy");
app.UseAuthentication(); 
app.UseAuthorization();

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "IceBreaker API v1");
    options.RoutePrefix = "swagger"; // Теперь будет доступно по /swagger
});

// Контроллеры
app.MapControllers();

// Health Check
app.MapGet("/api/health", () => Results.Ok(new
{
    status = "Healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
})).WithTags("Health");

// =============================================================================
// ЗАПУСК ПРИЛОЖЕНИЯ
// =============================================================================

try
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Запуск Ice Breaker API...");
    
    app.Run();
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "Критическая ошибка при запуске приложения");
    throw;
}