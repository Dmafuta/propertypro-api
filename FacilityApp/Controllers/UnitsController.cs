using FacilityApp.Data.Models;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/units")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Manager,Admin")]
public class UnitsController : ControllerBase
{
    private readonly IUnitService _units;

    public UnitsController(IUnitService units) => _units = units;

    // GET /api/units
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _units.GetAllAsync();
        return Ok(items.Select(d => new
        {
            d.Unit.Id, d.Unit.UnitNumber, d.Unit.Block, d.Unit.Floor,
            d.Unit.Description, d.Unit.IsOccupied,
            Owner    = d.Owner    is null ? null : new { d.Owner.Id,    d.Owner.FullName,    d.Owner.Email },
            Occupant = d.Occupant is null ? null : new { d.Occupant.Id, d.Occupant.FullName, d.Occupant.Email }
        }));
    }

    // GET /api/units/assignable?linkType=0
    [HttpGet("assignable")]
    public async Task<IActionResult> GetAssignable([FromQuery] int linkType = 1)
    {
        var users = await _units.GetAssignableUsersAsync((UnitLinkType)linkType);
        return Ok(users.Select(u => new { u.Id, u.FullName, u.Email }));
    }

    // POST /api/units
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUnitRequest req)
    {
        var unit = await _units.CreateAsync(req.UnitNumber, req.Block, req.Floor, req.Description);
        return Ok(unit);
    }

    // PUT /api/units/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUnitRequest req)
    {
        await _units.UpdateAsync(id, req.UnitNumber, req.Block, req.Floor, req.Description);
        return NoContent();
    }

    // DELETE /api/units/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _units.DeleteAsync(id);
        return NoContent();
    }

    // PUT /api/units/{id}/owner
    [HttpPut("{id:guid}/owner")]
    public async Task<IActionResult> AssignOwner(Guid id, [FromBody] AssignUserRequest req)
    {
        await _units.AssignOwnerAsync(id, req.UserId);
        return NoContent();
    }

    // DELETE /api/units/{id}/owner
    [HttpDelete("{id:guid}/owner")]
    public async Task<IActionResult> RemoveOwner(Guid id)
    {
        await _units.RemoveOwnerAsync(id);
        return NoContent();
    }

    // PUT /api/units/{id}/occupant
    [HttpPut("{id:guid}/occupant")]
    public async Task<IActionResult> AssignOccupant(Guid id, [FromBody] AssignUserRequest req)
    {
        await _units.AssignOccupantAsync(id, req.UserId);
        return NoContent();
    }

    // DELETE /api/units/{id}/occupant
    [HttpDelete("{id:guid}/occupant")]
    public async Task<IActionResult> RemoveOccupant(Guid id)
    {
        await _units.RemoveOccupantAsync(id);
        return NoContent();
    }
}
