using System.ComponentModel.DataAnnotations.Schema;

namespace FacilityApp.Data.Models;

public class Meter
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    public UtilityType UtilityType { get; set; }
    public MeterMode MeterMode { get; set; }

    public string MeterNumber { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Location { get; set; }       // e.g. "Main Distribution Board"
    public string? UnitOfMeasure { get; set; }  // kWh, m³, litres, etc.

    public bool IsActive { get; set; } = true;
    public DateTime InstallDate { get; set; } = DateTime.UtcNow;
    public DateTime? RetiredAt { get; set; }

    /// <summary>Link to the meter this one replaced (backward chain).</summary>
    public Guid? PreviousMeterId { get; set; }
    public Meter? PreviousMeter { get; set; }

    /// <summary>Link to the meter that replaced this one (forward chain).</summary>
    public Guid? ReplacedByMeterId { get; set; }
    public Meter? ReplacedByMeter { get; set; }

    /// <summary>Mode-specific configuration stored as JSONB. Shape varies by MeterMode.</summary>
    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }

    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MeterReading>  Readings      { get; set; } = [];
    public ICollection<PrepaidToken>  PrepaidTokens { get; set; } = [];
    public ICollection<MeterAlert>    Alerts        { get; set; } = [];
}
