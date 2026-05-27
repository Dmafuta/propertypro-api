using System.Security.Claims;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/entrances")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class EntrancesController : ControllerBase
{
    private readonly IEntranceService _entrances;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    public EntrancesController(IEntranceService entrances) => _entrances = entrances;

    // GET /api/entrances
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _entrances.GetAllAsync();
        return Ok(items);
    }

    // GET /api/entrances/active
    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        var items = await _entrances.GetActiveAsync();
        return Ok(items);
    }

    // POST /api/entrances
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateEntranceRequest req)
    {
        var item = await _entrances.CreateAsync(req.Name, req.Description);
        return Ok(item);
    }

    // PUT /api/entrances/{id}
    [HttpPut("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEntranceRequest req)
    {
        await _entrances.UpdateAsync(id, req.Name, req.Description);
        return NoContent();
    }

    // PATCH /api/entrances/{id}/toggle
    [HttpPatch("{id:guid}/toggle")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> Toggle(Guid id)
    {
        await _entrances.ToggleActiveAsync(id);
        return NoContent();
    }

    // DELETE /api/entrances/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _entrances.DeleteAsync(id);
        return NoContent();
    }

    // POST /api/entrances/select  (staff selects their active gate)
    [HttpPost("select")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Security,Receptionist,Manager,Admin")]
    public async Task<IActionResult> Select([FromBody] SetEntranceRequest req)
    {
        await _entrances.SetCurrentEntranceAsync(UserId, req.EntranceId);
        return NoContent();
    }
}
