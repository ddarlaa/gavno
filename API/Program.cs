using System.Reflection;
using FluentMigrator.Runner;
using FluentValidation;
using FluentValidation.AspNetCore;
using IceBreakerApp.API.Middleware;
using IceBreakerApp.Application.DTOs;
using IceBreakerApp.Application.DTOs.ListItem;
using IceBreakerApp.Application.DTOs.Response;
using IceBreakerApp.Application.DTOs.Update;
using IceBreakerApp.Application.IRepositories;
using Microsoft.OpenApi.Models;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using IceBreakerApp.Application.IServices;
using IceBreakerApp.Application.Services;
using IceBreakerApp.Domain;
using IceBreakerApp.Domain.IRepositories;
using IceBreakerApp.Domain.Models;
using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// =============================================================================
// КОНФИГУРАЦИЯ СЕРВИСОВ
// =============================================================================

// Логирование
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Настройка контекста базы данных для Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString,
        npgsqlOptions => npgsqlOptions.CommandTimeout(30)));

// Fluent Migrator - ВСЕГДА регистрируем 
builder.Services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddPostgres()
        .WithGlobalConnectionString(connectionString)  
        .ScanIn(typeof(Migrations.InitialCreate).Assembly).For.Migrations()) // Указываем, где искать миграции (атвоматически сканирует всю сборку)
    
    
    .AddLogging(lb => lb.AddFluentMigratorConsole());

// Контроллеры и JSON-серийализация
builder.Services.AddControllers()
    .AddNewtonsoftJson();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Ice Breaker API",
        Version = "v1",
        Description = "API для системы вопросов и ответов",
        Contact = new OpenApiContact
        {
            Name = "Ice Breaker App",
            Email = "support@icebreak.com"
        }
    });
    c.EnableAnnotations();
    
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// CORS
builder.Services.AddCors(options => options.AddPolicy("AllowAll", policy =>
{
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader();
}));

// Валидация
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();

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
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<ITopicRepository, TopicRepository>();
builder.Services.AddScoped<IQuestionAnswerRepository, QuestionAnswerRepository>();
builder.Services.AddScoped<IQuestionLikeRepository, QuestionLikeRepository>();

// Сервисы
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<ITopicService, TopicService>();
builder.Services.AddScoped<IQuestionAnswerService, QuestionAnswerService>();
builder.Services.AddScoped<IQuestionLikeService, QuestionLikeService>();


// =============================================================================
// FLUENTMIGRATOR МИГРАЦИИ 
// =============================================================================

var app = builder.Build();

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
        logger.LogInformation(" FluentMigrator миграции успешно применены");
    }
    else
    {
        logger.LogWarning(" Миграции не найдены. Создайте классы миграций.");
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, " Ошибка при применении FluentMigrator миграций");
    // Не падаем, продолжаем работу
}

// =============================================================================
// КОНФИГУРАЦИЯ PIPELINE
// =============================================================================

// Обработка исключений
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Разработческие инструменты
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ice Breaker API v1");
        c.RoutePrefix = string.Empty;
    });
}

// Безопасность
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();

// Маршрутизация
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