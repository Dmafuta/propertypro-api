namespace FacilityApp.Data.Models;

public class ConsumableType
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string Name { get; set; } = string.Empty;          // e.g. "Garbage Bags"
    public string Unit { get; set; } = string.Empty;          // e.g. "bags", "rolls"
    public int CurrentStock { get; set; }
    public int? LowStockThreshold { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ConsumableIssuance> Issuances { get; set; } = [];
}
