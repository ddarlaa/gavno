using Microsoft.AspNetCore.Authorization;

namespace IceBreakerApp.Application.Authorization.Requirements
{
    /// <summary>
    /// Требование владения ресурсом (пользователь может управлять только своими ресурсами)
    /// </summary>
    public class ResourceOwnerRequirement : IAuthorizationRequirement
    {
        public string ResourceType { get; }

        public ResourceOwnerRequirement(string resourceType)
        {
            ResourceType = resourceType;
        }
    }
}