using System.Security.Claims;
using FacilityApp.Data.Models;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/units")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class UnitsController(IUnitService units) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    // GET /api/units
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await units.GetAllAsync();
        return Ok(items.Select(Map));
    }

    // GET /api/units/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await units.GetByIdAsync(id);
        if (item is null) return NotFound();
        return Ok(MapDetail(item));
    }

    // GET /api/units/assignable/owners
    [HttpGet("assignable/owners")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager")]
    public async Task<IActionResult> GetAssignableOwners()
    {
        var users = await units.GetAssignableUsersAsync(UnitLinkType.Owner);
        return Ok(users.Select(u => new { u.Id, u.FullName, u.Email }));
    }

    // GET /api/units/assignable/occupants
    [HttpGet("assignable/occupants")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager")]
    public async Task<IActionResult> GetAssignableOccupants()
    {
        var users = await units.GetAssignableUsersAsync(UnitLinkType.Occupant);
        return Ok(users.Select(u => new { u.Id, u.FullName, u.Email, Type = u.UserType.ToString() }));
    }

    // POST /api/units
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateUnitRequest2 req)
    {
        if (string.IsNullOrWhiteSpace(req.UnitNumber))
            return BadRequest(new { error = "Unit number is required." });

        var unit = await units.CreateAsync(
            req.UnitNumber, req.Block, req.Floor, req.Description,
            req.UnitTypeId, (UnitStatus)req.Status,
            req.SizeM2, req.Bedrooms, req.Bathrooms,
            req.ParkingBays, req.MonthlyLevy, req.Notes);
        return Ok(new { unit.Id });
    }

    // PUT /api/units/{id}
    [HttpPut("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUnitRequest2 req)
    {
        if (string.IsNullOrWhiteSpace(req.UnitNumber))
            return BadRequest(new { error = "Unit number is required." });

        try
        {
            await units.UpdateAsync(id, req.UnitNumber, req.Block, req.Floor, req.Description,
                req.UnitTypeId, (UnitStatus)req.Status,
                req.SizeM2, req.Bedrooms, req.Bathrooms,
                req.ParkingBays, req.MonthlyLevy, req.Notes);
        }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
        return NoContent();
    }

    // PATCH /api/units/{id}/status
    [HttpPatch("{id:guid}/status")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager")]
    public async Task<IActionResult> PatchStatus(Guid id, [FromBody] PatchUnitStatusRequest req)
    {
        try { await units.PatchStatusAsync(id, (UnitStatus)req.Status); }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
        return NoContent();
    }

    // DELETE /api/units/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try { await units.DeleteAsync(id); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        return NoContent();
    }

    // PUT /api/units/{id}/owner
    [HttpPut("{id:guid}/owner")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager")]
    public async Task<IActionResult> SetOwner(Guid id, [FromBody] AssignUserRequest req)
    {
        try { await units.AssignOwnerAsync(id, req.UserId); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        return NoContent();
    }

    // DELETE /api/units/{id}/owner
    [HttpDelete("{id:guid}/owner")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager")]
    public async Task<IActionResult> RemoveOwner(Guid id)
    {
        await units.RemoveOwnerAsync(id);
        return NoContent();
    }

    // POST /api/units/{id}/occupants
    [HttpPost("{id:guid}/occupants")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager")]
    public async Task<IActionResult> AddOccupant(Guid id, [FromBody] AddOccupantRequest req)
    {
        try { await units.AddOccupantAsync(id, req.UserId, req.MoveInDate); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        return NoContent();
    }

    // DELETE /api/units/{id}/occupants/{userId}
    [HttpDelete("{id:guid}/occupants/{userId}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager")]
    public async Task<IActionResult> RemoveOccupant(Guid id, string userId, [FromBody] RemoveOccupantRequest? req)
    {
        try { await units.RemoveOccupantAsync(id, userId, req?.MoveOutDate); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        return NoContent();
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static object Map(UnitDetails d) => new
    {
        d.Unit.Id, d.Unit.UnitNumber, d.Unit.Block, d.Unit.Floor,
        d.Unit.Description, d.Unit.Status, StatusLabel = d.Unit.Status.ToString(),
        d.Unit.SizeM2, d.Unit.Bedrooms, d.Unit.Bathrooms, d.Unit.ParkingBays,
        d.Unit.MonthlyLevy, d.Unit.Notes, d.Unit.UnitTypeId,
        UnitTypeName = d.Unit.UnitType?.Name, d.Unit.CreatedAt,
        Owner     = d.Owner is null ? null : new { d.Owner.Id, d.Owner.FullName, d.Owner.Email },
        Occupants = d.Occupants.Select(o => new { o.Id, o.FullName, o.Email }),
        MeterCount = d.Unit.Meters?.Count ?? 0,
    };

    private static object MapDetail(UnitDetails d) => new
    {
        d.Unit.Id, d.Unit.UnitNumber, d.Unit.Block, d.Unit.Floor,
        d.Unit.Description, d.Unit.Status, StatusLabel = d.Unit.Status.ToString(),
        d.Unit.SizeM2, d.Unit.Bedrooms, d.Unit.Bathrooms, d.Unit.ParkingBays,
        d.Unit.MonthlyLevy, d.Unit.Notes, d.Unit.UnitTypeId,
        UnitTypeName       = d.Unit.UnitType?.Name,
        DefaultMonthlyLevy = d.Unit.UnitType?.DefaultMonthlyLevy,
        d.Unit.CreatedAt,
        Owner     = d.Owner is null ? null : new { d.Owner.Id, d.Owner.FullName, d.Owner.Email },
        Occupants = d.Occupants.Select(o => new { o.Id, o.FullName, o.Email }),
        AllUserUnits = d.Unit.UserUnits.Select(uu => new
        {
            uu.Id, uu.UserId, uu.User.FullName,
            LinkType = uu.LinkType.ToString(),
            uu.LinkedAt, uu.MoveInDate, uu.MoveOutDate,
        }),
        Meters = d.Unit.Meters?.Select(m => new
        {
            m.Id, m.MeterNumber, m.SerialNumber,
            UtilityType = m.UtilityType.ToString(),
            MeterMode   = m.MeterMode.ToString(),
            m.Location, m.UnitOfMeasure, m.IsActive, m.InstallDate, m.RetiredAt,
        }),
    };
}
