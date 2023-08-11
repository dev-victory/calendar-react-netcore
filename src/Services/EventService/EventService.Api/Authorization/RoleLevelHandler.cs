using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EventService.Api.Authorization
{
    public class RoleLevelHandler : AuthorizationHandler<RoleLevelRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RoleLevelRequirement requirement)
        {
            var role = context.User.Claims.FirstOrDefault(a => a.Type == ClaimTypes.Role)?.Value;

            if (role != requirement.RequiredPermissionRole.ToLower() && role != "admin")
            {
                context.Fail();
                return Task.CompletedTask;
            }

            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
