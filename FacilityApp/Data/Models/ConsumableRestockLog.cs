namespace FacilityApp.Data.Models;

public class ConsumableRestockLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid ConsumableTypeId { get; set; }
    public ConsumableType ConsumableType { get; set; } = null!;

    public int Quantity { get; set; }
    public string RestockedById { get; set; } = string.Empty;
    public ApplicationUser RestockedBy { get; set; } = null!;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
