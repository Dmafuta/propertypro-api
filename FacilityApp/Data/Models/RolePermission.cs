namespace FacilityApp.Data.Models;

/// <summary>
/// Maps an AppRole to a set of granular Permission flags.
/// Composite PK: (AppRoleId, Permission).
/// </summary>
public class RolePermission
{
    public Guid       AppRoleId  { get; set; }
    public Permission Permission { get; set; }

    public AppRole? AppRole { get; set; }
}
