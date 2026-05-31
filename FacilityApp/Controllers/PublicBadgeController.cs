using FacilityApp.Data;
using FacilityApp.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Controllers;

/// <summary>
/// Public (no auth) endpoint for the shareable visitor badge page.
/// The visit is looked up by ID only — no tenant filter applied — so the
/// frontend can render the badge without the visitor needing to log in.
/// </summary>
[ApiController]
[Route("api/badge")]
[AllowAnonymous]
public class PublicBadgeController : ControllerBase
{
    private readonly AppDbContext _db;

    public PublicBadgeController(AppDbContext db) => _db = db;

    // GET /api/badge/{visitId}
    [HttpGet("{visitId:guid}")]
    public async Task<IActionResult> GetBadge(Guid visitId)
    {
        // IgnoreQueryFilters so the tenant filter doesn't block the anonymous lookup
        var visit = await _db.Visits
            .IgnoreQueryFilters()
            .Include(v => v.Visitor)
            .Include(v => v.Host)
            .Include(v => v.EntryEntrance)
            .Include(v => v.Tenant)
            .FirstOrDefaultAsync(v => v.Id == visitId);

        if (visit is null) return NotFound();

        return Ok(new BadgeDto(
            visit.Id,
            visit.Visitor.FullName, visit.Visitor.Email, visit.Visitor.Phone, visit.Visitor.Company,
            visit.Visitor.PhotoUrl, visit.Purpose, visit.Host?.FullName,
            visit.ScheduledAt, visit.CheckedInAt,
            (int)visit.Status, visit.EntryEntrance?.Name,
            visit.Tenant?.Name ?? "", visit.Tenant?.LogoUrl, visit.Tenant?.PrimaryColour));
    }
}
