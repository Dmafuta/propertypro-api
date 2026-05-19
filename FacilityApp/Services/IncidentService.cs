using FacilityApp.Data;
using FacilityApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Services;

public class IncidentService : IIncidentService
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenantCtx;
    private readonly IAuditService _audit;

    public IncidentService(AppDbContext db, TenantContext tenantCtx, IAuditService audit)
    {
        _db        = db;
        _tenantCtx = tenantCtx;
        _audit     = audit;
    }

    public Task<List<IncidentReport>> GetAllAsync() =>
        _db.IncidentReports
           .Include(i => i.ReportedBy)
           .Include(i => i.ResolvedBy)
           .OrderByDescending(i => i.ReportedAt)
           .ToListAsync();

    public Task<int> GetOpenCountAsync() =>
        _db.IncidentReports
           .CountAsync(i => i.Status == IncidentStatus.Open || i.Status == IncidentStatus.UnderReview);

    public async Task<IncidentReport> CreateAsync(string title, string description, string location,
        string? involvedParties, IncidentCategory category, IncidentSeverity severity, string reportedById)
    {
        var incident = new IncidentReport
        {
            TenantId        = _tenantCtx.TenantId,
            Title           = title.Trim(),
            Description     = description.Trim(),
            Location        = location.Trim(),
            InvolvedParties = involvedParties?.Trim(),
            Category        = category,
            Severity        = severity,
            ReportedById    = reportedById
        };
        _db.IncidentReports.Add(incident);
        await _db.SaveChangesAsync();

        await _audit.LogAsync("IncidentLogged", "IncidentReport", incident.Id.ToString(),
            $"{severity} {category} incident: {title}", reportedById);

        return incident;
    }

    public async Task UpdateStatusAsync(Guid id, IncidentStatus status, string? resolutionNotes, string resolvedById)
    {
        var incident = await _db.IncidentReports.FindAsync(id)
            ?? throw new InvalidOperationException("Incident not found.");

        incident.Status          = status;
        incident.ResolutionNotes = resolutionNotes?.Trim();

        if (status is IncidentStatus.Resolved or IncidentStatus.Closed)
        {
            incident.ResolvedById = resolvedById;
            incident.ResolvedAt   = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        await _audit.LogAsync("IncidentStatusUpdated", "IncidentReport", id.ToString(),
            $"Status changed to {status}", resolvedById);
    }

    public async Task DeleteAsync(Guid id)
    {
        var incident = await _db.IncidentReports.FindAsync(id)
            ?? throw new InvalidOperationException("Incident not found.");
        _db.IncidentReports.Remove(incident);
        await _db.SaveChangesAsync();
    }
}
