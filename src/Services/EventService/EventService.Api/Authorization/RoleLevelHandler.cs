using Microsoft.AspNetCore.Authorization;

namespace EventService.Api.Authorization
{
    public class RoleLevelHandler : AuthorizationHandler<RoleLevelRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RoleLevelRequirement requirement)
        {
            var role = context.User.Claims.FirstOrDefault(a => a.Type == "Role")?.Value;

            if (role != requirement.RequiredPermissionRole)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            context.Succeed(requirement);
            return Task.CompletedTask;
        }
    }
}
