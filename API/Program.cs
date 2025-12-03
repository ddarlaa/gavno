using System.Reflection;
using FluentMigrator.Runner;
using FluentValidation;
using FluentValidation.AspNetCore;
using IceBreakerApp.API.Middleware;
using Microsoft.OpenApi.Models;
using Migrations;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using IceBreakerApp.Domain.IRepositories;
using IceBreakerApp.Application.IServices;
using IceBreakerApp.Application.Services;
using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 🛠️ 1. Конфигурация сервисов — ВСЁ до Build()

// 🔧 Fluent Migrator
builder.Services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddPostgres()
        .WithGlobalConnectionString(builder.Configuration.GetConnectionString("DefaultConnection"))
        .ScanIn(typeof(InitialCreate).Assembly).For.Migrations())
    .AddLogging(lb => lb.AddFluentMigratorConsole());

// 🧱 Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 📦 Контроллеры
builder.Services.AddControllers().AddNewtonsoftJson();

// 📘 Swagger
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

// 🔐 CORS
builder.Services.AddCors(options => options.AddPolicy("AllowAll", policy =>
    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// ✅ Валидация
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();

// 🧩 AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// 🧱 Репозитории и сервисы
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<ITopicRepository, TopicRepository>();
builder.Services.AddScoped<IQuestionAnswerRepository, QuestionAnswerRepository>();
builder.Services.AddScoped<IQuestionLikeRepository, QuestionLikeRepository>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<ITopicService, TopicService>();
builder.Services.AddScoped<IQuestionAnswerService, QuestionAnswerService>();
builder.Services.AddScoped<IQuestionLikeService, QuestionLikeService>();

// 🩺 Health Checks
builder.Services.AddHealthChecks();

// ✅ Формируем приложение
var app = builder.Build();

// 🚀 Применяем миграции
try
{
    using var scope = app.Services.CreateScope();
    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    runner.MigrateUp();
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Ошибка при применении миграций");
    throw;
}

// 🌐 Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ice Breaker API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "Healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
})).WithTags("Health");

app.Run();