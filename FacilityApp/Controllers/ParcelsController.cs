using System.Security.Claims;
using FacilityApp.Data.Models;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/parcels")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ParcelsController : ControllerBase
{
    private readonly IParcelService _parcels;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    public ParcelsController(IParcelService parcels) => _parcels = parcels;

    // GET /api/parcels?status=0
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? status = null)
    {
        ParcelStatus? s = status.HasValue ? (ParcelStatus)status.Value : null;
        var items = await _parcels.GetAllAsync(s);
        return Ok(items);
    }

    // GET /api/parcels/unit/{unitId}
    [HttpGet("unit/{unitId:guid}")]
    public async Task<IActionResult> GetForUnit(Guid unitId)
    {
        var items = await _parcels.GetForUnitAsync(unitId);
        return Ok(items);
    }

    // GET /api/parcels/pending-count
    [HttpGet("pending-count")]
    public async Task<IActionResult> PendingCount()
    {
        var count = await _parcels.GetPendingCountAsync();
        return Ok(new { count });
    }

    // POST /api/parcels
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Receptionist,Manager,Admin")]
    public async Task<IActionResult> Receive([FromBody] ReceiveParcelRequest req)
    {
        var parcel = await _parcels.ReceiveAsync(
            req.UnitId, req.RecipientName, req.CourierName,
            req.Description, UserId, req.Notes);
        return Ok(parcel);
    }

    // PATCH /api/parcels/{id}/collect
    [HttpPatch("{id:guid}/collect")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Receptionist,Manager,Admin")]
    public async Task<IActionResult> Collect(Guid id, [FromBody] CollectParcelRequest req)
    {
        await _parcels.MarkCollectedAsync(id, req.CollectedByName);
        return NoContent();
    }

    // PATCH /api/parcels/{id}/return
    [HttpPatch("{id:guid}/return")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Receptionist,Manager,Admin")]
    public async Task<IActionResult> Return(Guid id)
    {
        await _parcels.MarkReturnedAsync(id);
        return NoContent();
    }
}
