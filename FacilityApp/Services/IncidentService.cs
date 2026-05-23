using FacilityApp.Data;
using FacilityApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Services;

public class IncidentService : IIncidentService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly TenantContext _tenantCtx;
    private readonly IAuditService _audit;

    public IncidentService(IDbContextFactory<AppDbContext> factory, TenantContext tenantCtx, IAuditService audit)
    {
        _factory   = factory;
        _tenantCtx = tenantCtx;
        _audit     = audit;
    }

    public async Task<List<IncidentReport>> GetAllAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.IncidentReports
           .Include(i => i.ReportedBy)
           .Include(i => i.ResolvedBy)
           .OrderByDescending(i => i.ReportedAt)
           .ToListAsync();
    }

    public async Task<int> GetOpenCountAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.IncidentReports
           .CountAsync(i => i.Status == IncidentStatus.Open || i.Status == IncidentStatus.UnderReview);
    }

    public async Task<IncidentReport> CreateAsync(string title, string description, string location,
        string? involvedParties, IncidentCategory category, IncidentSeverity severity, string reportedById)
    {
        await using var db = await _factory.CreateDbContextAsync();
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
        db.IncidentReports.Add(incident);
        await db.SaveChangesAsync();

        await _audit.LogAsync("IncidentLogged", "IncidentReport", incident.Id.ToString(),
            $"{severity} {category} incident: {title}", reportedById);

        return incident;
    }

    public async Task UpdateStatusAsync(Guid id, IncidentStatus status, string? resolutionNotes, string resolvedById)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var incident = await db.IncidentReports.FindAsync(id)
            ?? throw new InvalidOperationException("Incident not found.");

        incident.Status          = status;
        incident.ResolutionNotes = resolutionNotes?.Trim();

        if (status is IncidentStatus.Resolved or IncidentStatus.Closed)
        {
            incident.ResolvedById = resolvedById;
            incident.ResolvedAt   = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();

        await _audit.LogAsync("IncidentStatusUpdated", "IncidentReport", id.ToString(),
            $"Status changed to {status}", resolvedById);
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var incident = await db.IncidentReports.FindAsync(id)
            ?? throw new InvalidOperationException("Incident not found.");
        db.IncidentReports.Remove(incident);
        await db.SaveChangesAsync();
    }
}
