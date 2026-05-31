namespace FacilityApp.Data.Models;

public class PrepaidToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    public Guid MeterId { get; set; }
    public Meter Meter { get; set; } = null!;

    public string TokenCode { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public decimal? UnitsLoaded { get; set; }  // kWh / m³ etc. — may not always be known

    public DateTime PurchasedAt { get; set; }
    public DateTime? LoadedAt { get; set; }    // when resident physically entered it on the meter

    public string? PurchasedByUserId { get; set; }
    public ApplicationUser? PurchasedBy { get; set; }

    public string? VoucherReference { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
