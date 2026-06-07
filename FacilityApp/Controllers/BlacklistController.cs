using System.Security.Claims;
using FacilityApp.Data.Models;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/blacklist")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class BlacklistController(IBlacklistService blacklist) : ControllerBase
{
    private string UserId   => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    private string UserName => User.FindFirstValue(ClaimTypes.Name) ?? "";

    // GET /api/blacklist?search=&type=&page=1&pageSize=25
    [HttpGet]
    public async Task<IActionResult> GetEntries(
        [FromQuery] string? search,
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var (items, total) = await blacklist.GetEntriesAsync(search, type, page, pageSize);
        return Ok(new
        {
            total, page, pageSize,
            items = items.Select(e => new
            {
                e.Id, e.FullName, e.Email, e.Phone, e.Reason,
                EntryType = e.EntryType.ToString(),
                e.AddedByName, e.AddedAt, e.ExpiresAt, e.Notes,
            }),
        });
    }

    // POST /api/blacklist
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager,Security")]
    public async Task<IActionResult> Add([FromBody] AddBlacklistRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.FullName)) return BadRequest(new { error = "Full name is required." });
        if (string.IsNullOrWhiteSpace(req.Reason))   return BadRequest(new { error = "Reason is required." });

        var entry = await blacklist.AddAsync(
            req.FullName, req.Email, req.Phone,
            req.Reason, (BlacklistType)req.EntryType,
            req.ExpiresAt, req.Notes,
            UserId, UserName);

        return Ok(new { entry.Id });
    }

    // DELETE /api/blacklist/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager")]
    public async Task<IActionResult> Remove(Guid id)
    {
        try { await blacklist.RemoveAsync(id); }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
        return NoContent();
    }
}

public record AddBlacklistRequest(
    string FullName, string? Email, string? Phone,
    string Reason, int EntryType,
    DateTime? ExpiresAt, string? Notes);
