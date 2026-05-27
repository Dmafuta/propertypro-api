using System.Security.Claims;
using FacilityApp.Data;
using FacilityApp.Data.Models;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/resident")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ResidentController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IVisitorService                 _visitors;
    private readonly IMaintenanceService             _maintenance;
    private readonly IParkingService                 _parking;
    private readonly IQrCodeService                  _qr;
    private readonly IAnnouncementService            _announcements;
    private readonly IUnitRequestService             _unitRequests;
    private readonly TenantContext                   _tenantCtx;
    private readonly UserManager<ApplicationUser>    _users;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    public ResidentController(
        IDbContextFactory<AppDbContext> factory,
        IVisitorService visitors,
        IMaintenanceService maintenance,
        IParkingService parking,
        IQrCodeService qr,
        IAnnouncementService announcements,
        IUnitRequestService unitRequests,
        TenantContext tenantCtx,
        UserManager<ApplicationUser> users)
    {
        _factory       = factory;
        _visitors      = visitors;
        _maintenance   = maintenance;
        _parking       = parking;
        _qr            = qr;
        _announcements = announcements;
        _unitRequests  = unitRequests;
        _tenantCtx     = tenantCtx;
        _users         = users;
    }

    // ── Dashboard ────────────────────────────────────────────────────────────

    // GET /api/resident/dashboard
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var userId = UserId;
        await using var db = await _factory.CreateDbContextAsync();
        var now = DateTime.UtcNow;

        var upcomingCount = await db.Visits.CountAsync(v =>
            v.HostUserId == userId && v.Status == VisitStatus.Scheduled && v.ScheduledAt >= now);
        var activeCount   = await db.Visits.CountAsync(v =>
            v.HostUserId == userId && v.Status == VisitStatus.CheckedIn);
        var totalCount    = await db.Visits.CountAsync(v => v.HostUserId == userId);

        var unitLink = await db.UserUnits
            .Include(uu => uu.Unit)
            .FirstOrDefaultAsync(uu => uu.UserId == userId);

        var pendingParcels = unitLink is not null
            ? await db.Parcels.CountAsync(p =>
                p.UnitId == unitLink.UnitId && p.Status == ParcelStatus.Pending)
            : 0;

        var openMaintenance = await db.MaintenanceRequests.CountAsync(m =>
            m.ResidentId == userId &&
            (m.Status == MaintenanceStatus.Open || m.Status == MaintenanceStatus.InProgress));

        var pendingRequest = await db.UnitRequests.AnyAsync(r =>
            r.ResidentId == userId && r.Status == UnitRequestStatus.Pending);

        var upcomingList = await db.Visits
            .Include(v => v.Visitor)
            .Where(v => v.HostUserId == userId && v.Status == VisitStatus.Scheduled && v.ScheduledAt >= now)
            .OrderBy(v => v.ScheduledAt)
            .Take(5)
            .Select(v => new ResidentUpcomingVisitDto(v.Id, v.Visitor.FullName, v.Purpose, v.ScheduledAt))
            .ToListAsync();

        return Ok(new ResidentDashboardResponse(
            upcomingCount, activeCount, totalCount,
            pendingParcels, openMaintenance,
            unitLink is not null, unitLink?.Unit.UnitNumber, pendingRequest,
            upcomingList));
    }

    // ── Visits ───────────────────────────────────────────────────────────────

    // GET /api/resident/visits?status=Scheduled
    [HttpGet("visits")]
    public async Task<IActionResult> GetVisits([FromQuery] string? status = null)
    {
        var visits = await _visitors.GetVisitsForHostAsync(UserId);

        if (status is not null &&
            Enum.TryParse<VisitStatus>(status, ignoreCase: true, out var parsed))
        {
            visits = visits.Where(v => v.Status == parsed).ToList();
        }

        var dtos = visits.Select(v => new ResidentVisitDto(
            v.Id,
            v.Visitor.FullName,
            v.Visitor.Phone,
            v.Purpose,
            v.Status.ToString(),
            v.ScheduledAt,
            v.CheckedInAt,
            v.CheckedOutAt,
            v.Status == VisitStatus.Scheduled ? _qr.GenerateVisitQr(v.Id) : null));

        return Ok(dtos);
    }

    // POST /api/resident/visits/pre-register
    [HttpPost("visits/pre-register")]
    public async Task<IActionResult> PreRegister([FromBody] ResidentPreRegisterRequest req)
    {
        var visit = await _visitors.PreRegisterAsync(
            req.VisitorName,
            req.VisitorEmail ?? "",
            req.VisitorPhone,
            null,
            req.Purpose,
            UserId,
            req.ScheduledAt);

        return Ok(visit);
    }

    // DELETE /api/resident/visits/{id}
    [HttpDelete("visits/{id:guid}")]
    public async Task<IActionResult> CancelVisit(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var visit = await db.Visits.FindAsync(id);
        if (visit is null) return NotFound();
        if (visit.HostUserId != UserId) return Forbid();
        if (visit.Status != VisitStatus.Scheduled)
            return BadRequest(new { error = "Only scheduled visits can be cancelled." });

        await _visitors.CancelAsync(id);
        return NoContent();
    }

    // ── Maintenance ──────────────────────────────────────────────────────────

    // GET /api/resident/maintenance
    [HttpGet("maintenance")]
    public async Task<IActionResult> GetMaintenance()
    {
        var items = await _maintenance.GetForResidentAsync(UserId);
        return Ok(items);
    }

    // POST /api/resident/maintenance
    [HttpPost("maintenance")]
    public async Task<IActionResult> SubmitMaintenance([FromBody] SubmitMaintenanceRequest req)
    {
        var item = await _maintenance.SubmitAsync(
            UserId, req.UnitId, req.Title, req.Description,
            (MaintenanceCategory)req.Category, (MaintenancePriority)req.Priority);
        return Ok(item);
    }

    // ── Parcels ──────────────────────────────────────────────────────────────

    // GET /api/resident/parcels?status=Pending
    [HttpGet("parcels")]
    public async Task<IActionResult> GetParcels([FromQuery] string? status = null)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var unitLink = await db.UserUnits.FirstOrDefaultAsync(uu => uu.UserId == UserId);
        if (unitLink is null)
            return Ok(Array.Empty<object>());

        var query = db.Parcels
            .Where(p => p.UnitId == unitLink.UnitId)
            .OrderByDescending(p => p.ReceivedAt);

        if (status is not null &&
            Enum.TryParse<ParcelStatus>(status, ignoreCase: true, out var parsed))
        {
            var items = await query.Where(p => p.Status == parsed).ToListAsync();
            return Ok(items);
        }

        return Ok(await query.ToListAsync());
    }

    // ── Vehicles ─────────────────────────────────────────────────────────────

    // GET /api/resident/vehicles
    [HttpGet("vehicles")]
    public async Task<IActionResult> GetVehicles()
    {
        var vehicles = await _parking.GetVehiclesForResidentAsync(UserId);
        var dtos = vehicles.Select(v => new ResidentVehicleDto(
            v.Id,
            v.PlateNumber,
            v.Make,
            v.Model,
            v.Colour,
            v.Tag?.TagNumber,
            v.Tag?.Status.ToString()));
        return Ok(dtos);
    }

    // POST /api/resident/vehicles
    [HttpPost("vehicles")]
    public async Task<IActionResult> RegisterVehicle([FromBody] ResidentVehicleInput req)
    {
        var vehicle = await _parking.RegisterVehicleAsync(
            UserId,
            req.LicensePlate,
            req.Make ?? "",
            req.Model ?? "",
            req.Colour ?? "",
            VehicleType.Car,
            null);
        return Ok(new ResidentVehicleDto(
            vehicle.Id,
            vehicle.PlateNumber,
            vehicle.Make,
            vehicle.Model,
            vehicle.Colour,
            null, null));
    }

    // ── Profile ──────────────────────────────────────────────────────────────

    // GET /api/resident/profile
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var user = await _users.FindByIdAsync(UserId);
        if (user is null) return NotFound();

        await using var db = await _factory.CreateDbContextAsync();
        var unitLink = await db.UserUnits
            .Include(uu => uu.Unit)
            .FirstOrDefaultAsync(uu => uu.UserId == UserId);

        return Ok(new ResidentProfileDto(
            user.Id,
            user.FullName,
            user.Email ?? "",
            user.PhoneNumber,
            unitLink?.Unit.UnitNumber));
    }

    // PUT /api/resident/profile
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateResidentProfileRequest req)
    {
        var user = await _users.FindByIdAsync(UserId);
        if (user is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(req.FullName))
            user.FullName = req.FullName.Trim();

        if (req.PhoneNumber is not null)
            user.PhoneNumber = req.PhoneNumber.Trim();

        var result = await _users.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { error = result.Errors.FirstOrDefault()?.Description ?? "Update failed." });

        return NoContent();
    }

    // POST /api/resident/profile/change-password
    [HttpPost("profile/change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var user = await _users.FindByIdAsync(UserId);
        if (user is null) return NotFound();

        var result = await _users.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { error = result.Errors.FirstOrDefault()?.Description ?? "Password change failed." });

        return NoContent();
    }

    // ── Documents ────────────────────────────────────────────────────────────

    // GET /api/resident/documents
    [HttpGet("documents")]
    public async Task<IActionResult> GetDocuments()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var items = await db.Documents
            .Where(d => d.IsActive)
            .OrderByDescending(d => d.UploadedAt)
            .ToListAsync();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var slug    = _tenantCtx.TenantSlug;
        var dtos    = items.Select(d => new ResidentDocumentDto(
            d.Id,
            d.Title,
            d.Category.ToString(),
            $"{baseUrl}/documents/{slug}/{d.StoredFileName}",
            d.UploadedAt));

        return Ok(dtos);
    }

    // ── Announcements ────────────────────────────────────────────────────────

    // GET /api/resident/announcements
    [HttpGet("announcements")]
    public async Task<IActionResult> GetAnnouncements()
    {
        var items = await _announcements.GetActiveAsync();
        var dtos  = items.Select(a => new ResidentAnnouncementDto(
            a.Id, a.Title, a.Body, a.Category.ToString(),
            a.PublishedAt, a.ExpiresAt));
        return Ok(dtos);
    }

    // ── Unit Requests ────────────────────────────────────────────────────────

    // GET /api/resident/unit-request
    [HttpGet("unit-request")]
    public async Task<IActionResult> GetUnitRequest()
    {
        var req = await _unitRequests.GetForResidentAsync(UserId);
        if (req is null) return Ok(null);

        return Ok(new ResidentUnitRequestDto(
            req.Id, req.UnitId, req.Unit.UnitNumber,
            req.Status.ToString(), req.Note, req.ReviewNote,
            req.RequestedAt, req.ReviewedAt));
    }

    // POST /api/resident/unit-request
    [HttpPost("unit-request")]
    public async Task<IActionResult> SubmitUnitRequest([FromBody] SubmitUnitRequestRequest req)
    {
        try
        {
            var result = await _unitRequests.SubmitAsync(UserId, req.UnitId, req.Note);
            return Ok(new ResidentUnitRequestDto(
                result.Id, result.UnitId, result.Unit.UnitNumber,
                result.Status.ToString(), result.Note, result.ReviewNote,
                result.RequestedAt, result.ReviewedAt));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
