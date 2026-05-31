namespace FacilityApp.Data.Models;

/// <summary>
/// Links a user to a unit. A unit can have multiple occupants simultaneously.
///   - HomeOwner with tenants:        Owner link only
///   - HomeOwner living in own unit:  Owner link + Occupant link
///   - Pure Resident:                 Occupant link only
///   - Families / couples:            Multiple Occupant links on the same unit
/// </summary>
public class UserUnit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public Guid UnitId { get; set; }
    public Unit Unit { get; set; } = null!;
    public UnitLinkType LinkType { get; set; }
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;

    // Tenancy tracking
    public DateTime? MoveInDate { get; set; }
    public DateTime? MoveOutDate { get; set; }
}
