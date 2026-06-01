using System.Security.Claims;
using FacilityApp.Data.Models;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/superadmin/announcements")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin")]
public class SuperAdminAnnouncementsController : ControllerBase
{
    private readonly IPlatformAnnouncementService _svc;

    private string UserId   => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    private string UserName => User.FindFirstValue(ClaimTypes.Name) ?? "SuperAdmin";

    public SuperAdminAnnouncementsController(IPlatformAnnouncementService svc) => _svc = svc;

    // GET /api/superadmin/announcements
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _svc.GetAllAsync();
        return Ok(items.Select(ToDto));
    }

    // POST /api/superadmin/announcements
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePlatformAnnouncementRequest req)
    {
        var item = await _svc.CreateAsync(
            req.Title,
            req.Body,
            (AnnouncementCategory)req.Category,
            (PlatformAudienceFilter)req.Audience,
            req.ExpiresAt,
            UserId,
            UserName);
        return Ok(ToDto(item));
    }

    // PATCH /api/superadmin/announcements/{id}/toggle
    [HttpPatch("{id:guid}/toggle")]
    public async Task<IActionResult> Toggle(Guid id)
    {
        await _svc.ToggleActiveAsync(id);
        return NoContent();
    }

    // DELETE /api/superadmin/announcements/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _svc.DeleteAsync(id);
        return NoContent();
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static object ToDto(PlatformAnnouncement a) => new
    {
        a.Id,
        a.Title,
        a.Body,
        Category  = (int)a.Category,
        Audience  = (int)a.Audience,
        a.IsActive,
        a.PublishedAt,
        a.ExpiresAt,
        CreatedBy = new { Id = a.CreatedById, FullName = a.CreatedByName },
    };
}

public record CreatePlatformAnnouncementRequest(
    string    Title,
    string    Body,
    int       Category,
    int       Audience,
    DateTime? ExpiresAt);
