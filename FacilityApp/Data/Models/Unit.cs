namespace FacilityApp.Data.Models;

public class Unit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string UnitNumber { get; set; } = string.Empty;
    public string? Block { get; set; }
    public string? Floor { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }

    // Unit type (optional — tenant defines their own types)
    public Guid? UnitTypeId { get; set; }
    public UnitType? UnitType { get; set; }

    // Rich attributes
    public UnitStatus Status { get; set; } = UnitStatus.Available;
    public decimal? SizeM2 { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public int ParkingBays { get; set; } = 0;

    /// <summary>Per-unit levy override. Falls back to UnitType.DefaultMonthlyLevy when null.</summary>
    public decimal? MonthlyLevy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserUnit> UserUnits { get; set; } = [];
    public ICollection<Meter>    Meters    { get; set; } = [];
}
