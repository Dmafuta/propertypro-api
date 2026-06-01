using FacilityApp.Data.Models;
using Microsoft.AspNetCore.Authorization;

namespace FacilityApp.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public Permission Permission { get; }

    public PermissionRequirement(Permission permission)
    {
        Permission = permission;
    }
}
