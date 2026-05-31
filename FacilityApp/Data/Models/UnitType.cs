namespace FacilityApp.Data.Models;

public class UnitType
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? DefaultMonthlyLevy { get; set; }
    public int? DefaultBedrooms { get; set; }
    public int? DefaultBathrooms { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Unit> Units { get; set; } = [];
}
