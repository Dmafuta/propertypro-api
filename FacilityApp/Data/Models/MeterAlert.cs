namespace FacilityApp.Data.Models;

public class MeterAlert
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }

    public Guid MeterId { get; set; }
    public Meter Meter { get; set; } = null!;

    public AlertType AlertType { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;

    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedAt { get; set; }
    public string? AcknowledgedByUserId { get; set; }
    public ApplicationUser? AcknowledgedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
