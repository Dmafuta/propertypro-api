using FacilityApp.Data.Models;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/passes")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class PassesController(IAccessPassService passes) : ControllerBase
{
    // GET /api/passes?status=&search=&page=1&pageSize=25
    [HttpGet]
    public async Task<IActionResult> GetPasses(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var (items, total) = await passes.GetPassesAsync(status, search, page, pageSize);
        return Ok(new
        {
            total, page, pageSize,
            items = items.Select(MapPass),
        });
    }

    // GET /api/passes/visit/{visitId}
    [HttpGet("visit/{visitId:guid}")]
    public async Task<IActionResult> GetForVisit(Guid visitId)
    {
        var pass = await passes.GetForVisitAsync(visitId);
        if (pass is null) return NotFound();
        return Ok(MapPass(pass));
    }

    // POST /api/passes
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager,Security,Receptionist")]
    public async Task<IActionResult> Generate([FromBody] GeneratePassRequest req)
    {
        try
        {
            var pass = await passes.GenerateAsync(
                req.VisitId, (PassType)req.PassType,
                req.VehicleRegistration, req.ParkingBay, req.ValidUntil);
            return Ok(MapPass(pass));
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // PATCH /api/passes/{id}/revoke
    [HttpPatch("{id:guid}/revoke")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager,Security")]
    public async Task<IActionResult> Revoke(Guid id, [FromBody] RevokePassRequest req)
    {
        try { await passes.RevokeAsync(id, req.Reason); }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
        return NoContent();
    }

    private static object MapPass(AccessPass p)
    {
        var now = DateTime.UtcNow;
        string passStatus = p.IsRevoked ? "Revoked"
            : p.ValidUntil.HasValue && p.ValidUntil <= now ? "Expired"
            : "Active";

        return new
        {
            p.Id, p.PassNumber,
            PassType            = p.PassType.ToString(),
            PassTypeValue       = (int)p.PassType,
            p.VehicleRegistration, p.ParkingBay,
            p.ValidFrom, p.ValidUntil,
            p.IsRevoked, p.RevokedAt, p.RevokedReason,
            p.CreatedAt,
            Status              = passStatus,
            VisitorName         = p.Visit?.Visitor?.FullName,
            VisitorEmail        = p.Visit?.Visitor?.Email,
            VisitorPhone        = p.Visit?.Visitor?.Phone,
            VisitId             = p.VisitId,
            VisitPurpose        = p.Visit?.Purpose,
            EntranceName        = p.Entrance?.Name,
        };
    }
}

public record GeneratePassRequest(
    Guid VisitId, int PassType,
    string? VehicleRegistration, string? ParkingBay, DateTime? ValidUntil);

public record RevokePassRequest(string? Reason);
