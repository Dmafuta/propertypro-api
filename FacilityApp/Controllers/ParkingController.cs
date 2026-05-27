using System.Security.Claims;
using FacilityApp.Data.Models;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/parking")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ParkingController : ControllerBase
{
    private readonly IParkingService _parking;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    public ParkingController(IParkingService parking) => _parking = parking;

    // ── Vehicles ──────────────────────────────────────────────────────────────

    // GET /api/parking/vehicles
    [HttpGet("vehicles")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Security,Manager,Admin")]
    public async Task<IActionResult> GetVehicles()
    {
        var items = await _parking.GetAllVehiclesAsync();
        return Ok(items);
    }

    // GET /api/parking/vehicles/my  (resident)
    [HttpGet("vehicles/my")]
    public async Task<IActionResult> GetMyVehicles()
    {
        var items = await _parking.GetVehiclesForResidentAsync(UserId);
        return Ok(items);
    }

    // POST /api/parking/vehicles
    [HttpPost("vehicles")]
    public async Task<IActionResult> RegisterVehicle([FromBody] RegisterVehicleRequest req)
    {
        var vehicle = await _parking.RegisterVehicleAsync(
            req.OwnerId, req.Plate, req.Make, req.Model,
            req.Colour, (VehicleType)req.Type, req.Notes);
        return Ok(vehicle);
    }

    // DELETE /api/parking/vehicles/{id}
    [HttpDelete("vehicles/{id:guid}")]
    public async Task<IActionResult> DeleteVehicle(Guid id)
    {
        await _parking.DeleteVehicleAsync(id);
        return NoContent();
    }

    // ── Tags ──────────────────────────────────────────────────────────────────

    // POST /api/parking/tags
    [HttpPost("tags")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Manager,Admin")]
    public async Task<IActionResult> IssueTag([FromBody] IssueTagRequest req)
    {
        var tag = await _parking.IssueTagAsync(req.VehicleId, UserId, req.ExpiresAt, req.Notes);
        return Ok(tag);
    }

    // PATCH /api/parking/tags/{id}/status
    [HttpPatch("tags/{id:guid}/status")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Manager,Admin")]
    public async Task<IActionResult> UpdateTagStatus(Guid id, [FromBody] UpdateTagStatusRequest req)
    {
        await _parking.UpdateTagStatusAsync(id, (TagStatus)req.Status);
        return NoContent();
    }

    // GET /api/parking/tags/lookup?tagNumber=TAG-0001
    [HttpGet("tags/lookup")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Security,Manager,Admin")]
    public async Task<IActionResult> LookupTag([FromQuery] string tagNumber)
    {
        var tag = await _parking.LookupTagAsync(tagNumber);
        if (tag is null) return NotFound();
        return Ok(tag);
    }

    // ── Records ───────────────────────────────────────────────────────────────

    // GET /api/parking/active
    [HttpGet("active")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Security,Manager,Admin")]
    public async Task<IActionResult> GetActive()
    {
        var items = await _parking.GetCurrentlyInsideAsync();
        return Ok(items);
    }

    // GET /api/parking/history?from=2026-01-01&to=2026-01-31
    [HttpGet("history")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Security,Manager,Admin")]
    public async Task<IActionResult> GetHistory([FromQuery] string? from, [FromQuery] string? to)
    {
        var fromDate = DateOnly.TryParse(from, out var fd) ? fd : DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var toDate   = DateOnly.TryParse(to,   out var td) ? td : DateOnly.FromDateTime(DateTime.Today);
        var items    = await _parking.GetRecordsAsync(fromDate, toDate);
        return Ok(items);
    }

    // POST /api/parking/entry/tag
    [HttpPost("entry/tag")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Security,Manager,Admin")]
    public async Task<IActionResult> LogEntryByTag([FromBody] LogEntryByTagRequest req)
    {
        var record = await _parking.LogEntryByTagAsync(req.TagNumber, UserId, req.EntranceId);
        return Ok(record);
    }

    // POST /api/parking/entry/visitor
    [HttpPost("entry/visitor")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Security,Manager,Admin")]
    public async Task<IActionResult> LogVisitorEntry([FromBody] LogVisitorEntryRequest req)
    {
        var record = await _parking.LogVisitorEntryAsync(req.Plate, UserId, req.VisitId, req.EntranceId, req.Notes);
        return Ok(record);
    }

    // PATCH /api/parking/records/{id}/exit
    [HttpPatch("records/{id:guid}/exit")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Security,Manager,Admin")]
    public async Task<IActionResult> LogExit(Guid id, [FromBody] LogExitRequest req)
    {
        await _parking.LogExitAsync(id, UserId, req.ExitEntranceId);
        return NoContent();
    }
}
