namespace FacilityApp.Data.Models;

public class MeterReading
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    public Guid MeterId { get; set; }
    public Meter Meter { get; set; } = null!;

    public decimal ReadingValue { get; set; }
    public DateTime ReadingDate { get; set; }
    public MeterReadingType ReadingType { get; set; }

    public string? ReadByUserId { get; set; }
    public ApplicationUser? ReadBy { get; set; }

    public string? PhotoUrl { get; set; }
    public string? Notes { get; set; }
    public bool IsVerified { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
