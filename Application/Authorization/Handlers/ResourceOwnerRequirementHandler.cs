using Microsoft.AspNetCore.Authorization;
using IceBreakerApp.Application.Authorization.Requirements;
using IceBreakerApp.Application.IRepositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Routing;

namespace IceBreakerApp.Application.Authorization.Handlers
{
    public class ResourceOwnerRequirementHandler : AuthorizationHandler<ResourceOwnerRequirement>
    {
        private readonly IUserRepository _userRepository;
        private readonly IQuestionRepository _questionRepository;
        private readonly IQuestionAnswerRepository _answerRepository;
        private readonly ILogger<ResourceOwnerRequirementHandler> _logger;

        public ResourceOwnerRequirementHandler(
            IUserRepository userRepository,
            IQuestionRepository questionRepository,
            IQuestionAnswerRepository answerRepository,
            ILogger<ResourceOwnerRequirementHandler> logger)
        {
            _userRepository = userRepository;
            _questionRepository = questionRepository;
            _answerRepository = answerRepository;
            _logger = logger;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context, 
            ResourceOwnerRequirement requirement)
        {
            try
            {
                // Проверяем, что пользователь аутентифицирован
                if (!context.User.Identity?.IsAuthenticated ?? true)
                {
                    _logger.LogWarning("User is not authenticated for resource ownership check");
                    return;
                }

                // Получаем ID пользователя из claims
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
                {
                    _logger.LogWarning("Cannot extract user ID from claims for resource ownership check");
                    return;
                }

                // Проверяем владение ресурсом
                var isOwner = requirement.ResourceType.ToLowerInvariant() switch
                {
                    "user" => await CheckUserOwnershipAsync(currentUserId, context),
                    "question" => await CheckQuestionOwnershipAsync(currentUserId, context),
                    "answer" => await CheckAnswerOwnershipAsync(currentUserId, context),
                    _ => false
                };

                if (isOwner)
                {
                    context.Succeed(requirement);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during resource ownership authorization check");
            }
        }

        private async Task<bool> CheckUserOwnershipAsync(Guid currentUserId, AuthorizationHandlerContext context)
        {
            if (context.Resource is HttpContext httpContext)
            {
                // СПОСОБ 1: Используем GetRouteData() (рекомендуется)
                var routeData = httpContext.GetRouteData();
                if (routeData == null) return false;
                
                var routeValues = routeData.Values;
                
                // СПОСОБ 2: Или через Features (альтернатива)
                // var routingFeature = httpContext.Features.Get<Microsoft.AspNetCore.Routing.IRoutingFeature>();
                // var routeValues = routingFeature?.RouteData?.Values;

                var userIdParam = GetRouteValue(routeValues, "id", "userId");
                if (!string.IsNullOrEmpty(userIdParam) && 
                    Guid.TryParse(userIdParam, out var requestedUserId))
                {
                    return currentUserId == requestedUserId;
                }
            }
            return false;
        }

        private async Task<bool> CheckQuestionOwnershipAsync(Guid currentUserId, AuthorizationHandlerContext context)
        {
            if (context.Resource is HttpContext httpContext)
            {
                var routeData = httpContext.GetRouteData();
                if (routeData == null) return false;
                
                var routeValues = routeData.Values;
                var questionIdParam = GetRouteValue(routeValues, "id", "questionId");

                if (!string.IsNullOrEmpty(questionIdParam) && 
                    Guid.TryParse(questionIdParam, out var questionId))
                {
                    var question = await _questionRepository.GetByIdAsync(questionId);
                    return question?.UserId == currentUserId;
                }
            }
            return false;
        }

        private async Task<bool> CheckAnswerOwnershipAsync(Guid currentUserId, AuthorizationHandlerContext context)
        {
            if (context.Resource is HttpContext httpContext)
            {
                var routeData = httpContext.GetRouteData();
                if (routeData == null) return false;
                
                var routeValues = routeData.Values;
                var answerIdParam = GetRouteValue(routeValues, "id", "answerId");

                if (!string.IsNullOrEmpty(answerIdParam) && 
                    Guid.TryParse(answerIdParam, out var answerId))
                {
                    var answer = await _answerRepository.GetByIdAsync(answerId);
                    return answer?.UserId == currentUserId;
                }
            }
            return false;
        }

        // Вспомогательный метод для получения значения из route values
        private string GetRouteValue(IReadOnlyDictionary<string, object> routeValues, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (routeValues.TryGetValue(key, out var value))
                {
                    return value?.ToString();
                }
            }
            return null;
        }
    }
}