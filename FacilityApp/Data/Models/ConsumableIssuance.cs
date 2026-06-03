namespace FacilityApp.Data.Models;

public class ConsumableIssuance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid ConsumableTypeId { get; set; }
    public ConsumableType ConsumableType { get; set; } = null!;

    public Guid UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    public int Quantity { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    public string IssuedById { get; set; } = string.Empty;
    public ApplicationUser IssuedBy { get; set; } = null!;

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
