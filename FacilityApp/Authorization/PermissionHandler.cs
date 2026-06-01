using System.Security.Claims;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authorization;

namespace FacilityApp.Authorization;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissions;

    public PermissionHandler(IPermissionService permissions)
    {
        _permissions = permissions;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return;

        // SuperAdmin bypasses all permission checks
        if (context.User.IsInRole("SuperAdmin"))
        {
            context.Succeed(requirement);
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return;

        var userPermissions = await _permissions.GetPermissionsAsync(userId);
        if (userPermissions.Contains(requirement.Permission))
            context.Succeed(requirement);
    }
}
