using FacilityApp.Data.Models;
using Microsoft.AspNetCore.Authorization;

namespace FacilityApp.Authorization;

/// <summary>
/// Declarative permission gate for controllers and actions.
/// Usage: [RequirePermission(Permission.CanManageUsers)]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(Permission permission)
        : base($"{PermissionPolicyProvider.PolicyPrefix}{permission}") { }
}
