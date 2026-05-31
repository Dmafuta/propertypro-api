using FacilityApp.Data;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FacilityApp.Data.Models;
using System.Security.Claims;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class DashboardController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly TenantContext                   _tenantCtx;

    public DashboardController(IDbContextFactory<AppDbContext> factory, TenantContext tenantCtx)
    {
        _factory   = factory;
        _tenantCtx = tenantCtx;
    }

    // GET /api/dashboard
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (_tenantCtx.TenantId == Guid.Empty)
            return Unauthorized(new { error = "Tenant context not resolved." });

        await using var db  = await _factory.CreateDbContextAsync();
        var now             = DateTime.UtcNow;
        var todayStart      = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var todayEnd        = todayStart.AddDays(1);

        var todayVisits     = await db.Visits.CountAsync(v =>
            (v.ScheduledAt >= todayStart && v.ScheduledAt < todayEnd) ||
            (v.CheckedInAt != null && v.CheckedInAt >= todayStart && v.CheckedInAt < todayEnd));

        var activeVisits    = await db.Visits.CountAsync(v => v.Status == VisitStatus.CheckedIn);
        var pendingParcels  = await db.Parcels.CountAsync(p => p.Status == ParcelStatus.Pending);
        var openMaintenance = await db.MaintenanceRequests.CountAsync(m =>
            m.Status == MaintenanceStatus.Open || m.Status == MaintenanceStatus.InProgress);

        var totalUnits           = await db.Units.CountAsync();
        var occupiedUnits        = await db.Units.CountAsync(u => u.Status == FacilityApp.Data.Models.UnitStatus.Occupied);
        var openIncidents        = await db.IncidentReports.CountAsync(r =>
            r.Status == IncidentStatus.Open || r.Status == IncidentStatus.UnderReview);
        var pendingUnitRequests  = await db.UnitRequests.CountAsync(r => r.Status == UnitRequestStatus.Pending);
        var activeVehicles       = await db.ParkingRecords.CountAsync(r => r.ExitedAt == null);

        var upcomingVisits = await db.Visits
            .Include(v => v.Visitor)
            .Include(v => v.Host)
            .Where(v => v.Status == VisitStatus.Scheduled && v.ScheduledAt >= now)
            .OrderBy(v => v.ScheduledAt)
            .Take(10)
            .Select(v => new UpcomingVisitDto(
                v.Id,
                v.Visitor.FullName,
                v.Purpose,
                v.ScheduledAt,
                v.Host != null ? v.Host.FullName : null))
            .ToListAsync();

        return Ok(new DashboardResponse(
            todayVisits, activeVisits, pendingParcels,
            openMaintenance, totalUnits, occupiedUnits,
            openIncidents, pendingUnitRequests, activeVehicles,
            upcomingVisits));
    }
}
