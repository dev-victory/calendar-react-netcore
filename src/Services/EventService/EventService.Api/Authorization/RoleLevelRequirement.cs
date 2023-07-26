using Microsoft.AspNetCore.Authorization;

namespace EventService.Api.Authorization
{
    public class RoleLevelRequirement : IAuthorizationRequirement
    {
        public string RequiredPermissionRole { get; set; }

        public RoleLevelRequirement(string requiredPermissionRole)
        {
            RequiredPermissionRole = requiredPermissionRole;
        }
    }
}