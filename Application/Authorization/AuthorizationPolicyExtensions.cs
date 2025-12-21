using Microsoft.AspNetCore.Authorization;
using IceBreakerApp.Application.Authorization.Requirements;

namespace IceBreakerApp.Application.Authorization
{
    /// <summary>
    /// Расширения для настройки политик авторизации
    /// </summary>
    public static class AuthorizationPolicyExtensions
    {
        public static AuthorizationPolicyBuilder RequireAdminRole(this AuthorizationPolicyBuilder builder)
        {
            return builder.RequireRole("Admin");
        }

        public static AuthorizationPolicyBuilder RequireModeratorOrAdmin(this AuthorizationPolicyBuilder builder)
        {
            return builder.RequireRole("Admin", "Moderator");
        }

        public static AuthorizationPolicyBuilder RequireEmailConfirmed(this AuthorizationPolicyBuilder builder)
        {
            return builder.RequireClaim("EmailConfirmed", "True");
        }

        public static AuthorizationPolicyBuilder RequirePremiumSubscription(this AuthorizationPolicyBuilder builder)
        {
            return builder.RequireClaim("SubscriptionLevel", "Premium", "Enterprise");
        }
        
        public static AuthorizationPolicyBuilder RequireResourceOwner(this AuthorizationPolicyBuilder builder, string resourceType)
        {
            return builder.AddRequirements(new ResourceOwnerRequirement(resourceType));
        }
    }
}