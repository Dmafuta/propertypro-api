using System.Security.Claims;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/consumables")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ConsumablesController(IConsumableService consumables) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    // ── Types ─────────────────────────────────────────────────────────────────

    // GET /api/consumables/types
    [HttpGet("types")]
    public async Task<IActionResult> GetTypes()
    {
        var types = await consumables.GetAllTypesAsync();
        return Ok(types.Select(t => new
        {
            t.Id, t.Name, t.Unit, t.CurrentStock, t.LowStockThreshold, t.IsActive, t.CreatedAt,
        }));
    }

    // POST /api/consumables/types
    [HttpPost("types")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateType([FromBody] CreateConsumableTypeRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { error = "Name is required." });
        if (string.IsNullOrWhiteSpace(req.Unit)) return BadRequest(new { error = "Unit is required." });
        var type = await consumables.CreateTypeAsync(req.Name, req.Unit, req.LowStockThreshold);
        return Ok(new { type.Id });
    }

    // PATCH /api/consumables/types/{id}/restock
    [HttpPatch("types/{id:guid}/restock")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager")]
    public async Task<IActionResult> Restock(Guid id, [FromBody] RestockRequest req)
    {
        try { await consumables.RestockAsync(id, req.Quantity, UserId, req.Notes); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        return NoContent();
    }

    // GET /api/consumables/restock-log?typeId=
    [HttpGet("restock-log")]
    public async Task<IActionResult> GetRestockLog([FromQuery] Guid? typeId)
    {
        var items = await consumables.GetRestockLogsAsync(typeId);
        return Ok(items.Select(r => new
        {
            r.Id,
            ConsumableTypeId   = r.ConsumableTypeId,
            ConsumableTypeName = r.ConsumableType.Name,
            ConsumableUnit     = r.ConsumableType.Unit,
            r.Quantity,
            RestockedBy        = r.RestockedBy.FullName,
            r.Notes,
            r.CreatedAt,
        }));
    }

    // PATCH /api/consumables/types/{id}/toggle
    [HttpPatch("types/{id:guid}/toggle")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager")]
    public async Task<IActionResult> Toggle(Guid id)
    {
        try { await consumables.ToggleTypeActiveAsync(id); }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
        return NoContent();
    }

    // ── Issuances ─────────────────────────────────────────────────────────────

    // GET /api/consumables/issuances?typeId=&unitId=&from=&to=
    [HttpGet("issuances")]
    public async Task<IActionResult> GetIssuances(
        [FromQuery] Guid? typeId,
        [FromQuery] Guid? unitId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var items = await consumables.GetIssuancesAsync(typeId, unitId, from, to);
        return Ok(items.Select(i => new
        {
            i.Id,
            ConsumableTypeId   = i.ConsumableTypeId,
            ConsumableTypeName = i.ConsumableType.Name,
            ConsumableUnit     = i.ConsumableType.Unit,
            UnitId             = i.UnitId,
            UnitNumber         = i.Unit.UnitNumber,
            Block              = i.Unit.Block,
            i.Quantity,
            i.IssuedAt,
            IssuedBy           = i.IssuedBy.FullName,
            i.Notes,
            i.CreatedAt,
        }));
    }

    // GET /api/consumables/issuances/unit/{unitId}
    [HttpGet("issuances/unit/{unitId:guid}")]
    public async Task<IActionResult> GetForUnit(Guid unitId)
    {
        var items = await consumables.GetIssuancesForUnitAsync(unitId);
        return Ok(items.Select(i => new
        {
            i.Id,
            ConsumableTypeId   = i.ConsumableTypeId,
            ConsumableTypeName = i.ConsumableType.Name,
            ConsumableUnit     = i.ConsumableType.Unit,
            i.Quantity,
            i.IssuedAt,
            IssuedBy           = i.IssuedBy.FullName,
            i.Notes,
            i.CreatedAt,
        }));
    }

    // POST /api/consumables/issue
    [HttpPost("issue")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager,Receptionist")]
    public async Task<IActionResult> Issue([FromBody] IssueConsumableRequest req)
    {
        try
        {
            var issuance = await consumables.IssueAsync(
                req.ConsumableTypeId, req.UnitId, req.Quantity,
                req.IssuedAt, UserId, req.Notes);
            return Ok(new { issuance.Id });
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }
}

public record CreateConsumableTypeRequest(string Name, string Unit, int? LowStockThreshold);
public record RestockRequest(int Quantity, string? Notes);
public record IssueConsumableRequest(Guid ConsumableTypeId, Guid UnitId, int Quantity, DateTime IssuedAt, string? Notes);
