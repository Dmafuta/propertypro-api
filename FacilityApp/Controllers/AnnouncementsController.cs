using System.Security.Claims;
using FacilityApp.Data.Models;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/announcements")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AnnouncementsController : ControllerBase
{
    private readonly IAnnouncementService _announcements;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    public AnnouncementsController(IAnnouncementService announcements) => _announcements = announcements;

    // GET /api/announcements        (admin — all)
    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Manager,Admin")]
    public async Task<IActionResult> GetAll()
    {
        var items = await _announcements.GetAllAsync();
        return Ok(items);
    }

    // GET /api/announcements/active  (residents & staff — active only)
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var items = await _announcements.GetActiveAsync();
        return Ok(items);
    }

    // POST /api/announcements
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Manager,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateAnnouncementRequest req)
    {
        var item = await _announcements.CreateAsync(
            req.Title, req.Body, (AnnouncementCategory)req.Category, req.ExpiresAt, UserId);
        return Ok(item);
    }

    // PATCH /api/announcements/{id}/toggle
    [HttpPatch("{id:guid}/toggle")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Manager,Admin")]
    public async Task<IActionResult> Toggle(Guid id)
    {
        await _announcements.ToggleActiveAsync(id);
        return NoContent();
    }

    // DELETE /api/announcements/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Manager,Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _announcements.DeleteAsync(id);
        return NoContent();
    }
}
