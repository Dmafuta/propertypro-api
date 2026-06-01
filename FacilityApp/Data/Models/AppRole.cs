namespace FacilityApp.Data.Models;

/// <summary>
/// Metadata for roles managed by SuperAdmin.
/// Name mirrors the corresponding AspNetRoles.Name entry.
/// </summary>
public class AppRole
{
    public Guid   Id          { get; set; } = Guid.NewGuid();
    public string Name        { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>System roles (seeded) cannot be deleted, only reconfigured.</summary>
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RolePermission> Permissions { get; set; } = [];
}
