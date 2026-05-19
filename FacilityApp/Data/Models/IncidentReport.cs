namespace FacilityApp.Data.Models;

public class IncidentReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string? InvolvedParties { get; set; }

    public IncidentCategory Category { get; set; } = IncidentCategory.Other;
    public IncidentSeverity Severity { get; set; } = IncidentSeverity.Medium;
    public IncidentStatus Status { get; set; } = IncidentStatus.Open;

    public string ReportedById { get; set; } = string.Empty;
    public ApplicationUser ReportedBy { get; set; } = null!;
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

    public string? ResolvedById { get; set; }
    public ApplicationUser? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNotes { get; set; }
}

public enum IncidentCategory  { Security, Medical, Fire, Disturbance, Vandalism, Maintenance, Other }
public enum IncidentSeverity  { Low, Medium, High, Critical }
public enum IncidentStatus    { Open, UnderReview, Resolved, Closed }
