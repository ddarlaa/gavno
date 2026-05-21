using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace IceBreakerApp.Application
{
    [Authorize]
    public class FileNotificationHub : Hub
    {
        private readonly ILogger<FileNotificationHub> _logger;

        public FileNotificationHub(ILogger<FileNotificationHub> logger)
        {
            _logger = logger;
        }

        public async Task<object> JoinUserGroup() // Изменено: возвращает Task<object>
        {
            try
            {
                var userId = GetUserIdFromClaims();
        
                _logger.LogInformation($"User joining group: {userId}, ConnectionId: {Context.ConnectionId}");
        
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims");
                    await Clients.Caller.SendAsync("Error", "User ID not found");
                    return new { success = false, error = "User ID not found" };
                }

                _logger.LogInformation($"Adding to group: {userId}");
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        
                _logger.LogInformation($"User {userId} joined notification group");
        
                // Отправляем подтверждение через уведомление
                var response = new 
                { 
                    userId,
                    success = true,
                    message = "Now you will receive file notifications",
                    connectionId = Context.ConnectionId
                };
        
                _logger.LogInformation($"📤 Sending JoinConfirmed to caller: {Context.ConnectionId}");
                await Clients.Caller.SendAsync("JoinConfirmed", response);
        
                _logger.LogInformation($"📨 JoinConfirmed sent successfully");
                
                // Возвращаем результат вызова метода
                return new 
                { 
                    success = true, 
                    userId,
                    connectionId = Context.ConnectionId,
                    message = "Successfully joined notification group"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ JoinUserGroup failed");
                return new { success = false, error = ex.Message };
            }
        }

        // Универсальный метод для получения userId из любых claims
        private string? GetUserIdFromClaims()
        {
            // Пробуем все возможные варианты по порядку
            var claims = Context.User?.Claims;
            if (claims == null) 
            {
                _logger.LogWarning("No claims found for user");
                return null;
            }

            // Логируем все claims для отладки
            _logger.LogInformation($"User has {claims.Count()} claims:");
            foreach (var claim in claims)
            {
                _logger.LogInformation($"  {claim.Type}: {claim.Value}");
            }

            // 1. Ищем прямо по значению GUID (самое надежное)
            foreach (var claim in claims)
            {
                if (Guid.TryParse(claim.Value, out var guid))
                {
                    _logger.LogInformation($"Found GUID userId: {claim.Value}");
                    return claim.Value;
                }
            }

            // 2. Ищем по известным claim типам
            var claimTypes = new[] 
            { 
                "nameid",           // Ваш случай
                ClaimTypes.NameIdentifier,
                "sub",
                "unique_name",
                ClaimTypes.Name,
                "userId",
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
            };

            foreach (var claimType in claimTypes)
            {
                var value = Context.User?.FindFirst(claimType)?.Value;
                if (!string.IsNullOrEmpty(value))
                {
                    _logger.LogInformation($"Found userId in claim {claimType}: {value}");
                    return value;
                }
            }

            _logger.LogWarning("No valid userId found in claims");
            return null;
        }

        // Тестовый метод - быстро проверить подключение
        public async Task<object> Ping() // Изменено: возвращает Task<object>
        {
            var userId = GetUserIdFromClaims();
            
            var response = new
            {
                timestamp = DateTime.UtcNow,
                connectionId = Context.ConnectionId,
                userId = userId
            };
            
            _logger.LogInformation($"Ping request from userId: {userId}");
            
            // Отправляем уведомление
            await Clients.Caller.SendAsync("Pong", response);
            
            // Возвращаем результат вызова
            return new 
            { 
                message = "Ping completed successfully",
                timestamp = response.timestamp 
            };
        }

        // Дополнительный тестовый метод, который точно возвращает строку
        public string TestConnection()
        {
            return $"Connection OK. ConnectionId: {Context.ConnectionId}, Time: {DateTime.UtcNow}";
        }

        // Override для логирования подключений
        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"🔗 Client connected: {Context.ConnectionId}, UserId: {GetUserIdFromClaims()}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"🔌 Client disconnected: {Context.ConnectionId}, Exception: {exception?.Message}");
            await base.OnDisconnectedAsync(exception);
        }
    }
}