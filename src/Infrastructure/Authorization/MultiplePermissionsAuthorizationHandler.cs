using Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Authorization;

internal sealed class MultiplePermissionsAuthorizationHandler(IServiceScopeFactory serviceScopeFactory)
    : AuthorizationHandler<MultiplePermissionsRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MultiplePermissionsRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        using IServiceScope scope = serviceScopeFactory.CreateScope();

        PermissionProvider permissionProvider = scope.ServiceProvider.GetRequiredService<PermissionProvider>();

        Guid userId = context.User.GetUserId();

        HashSet<string> userPermissions = await permissionProvider.GetForUserIdAsync(userId);

        // OR logic: إذا كان لديه أي صلاحية من الصلاحيات المطلوبة
        bool hasAnyPermission = requirement.Permissions.Any(permission => userPermissions.Contains(permission));

        if (hasAnyPermission)
        {
            context.Succeed(requirement);
        }
    }
}


