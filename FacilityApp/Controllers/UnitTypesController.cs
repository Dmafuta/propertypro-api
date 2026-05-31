using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/unit-types")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class UnitTypesController(IUnitTypeService unitTypes) : ControllerBase
{
    // GET /api/unit-types
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await unitTypes.GetAllAsync();
        return Ok(items.Select(t => new
        {
            t.Id, t.Name, t.Description, t.DefaultMonthlyLevy,
            t.DefaultBedrooms, t.DefaultBathrooms, t.IsActive, t.CreatedAt,
        }));
    }

    // POST /api/unit-types
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateUnitTypeRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { error = "Name is required." });

        var item = await unitTypes.CreateAsync(req.Name, req.Description,
            req.DefaultMonthlyLevy, req.DefaultBedrooms, req.DefaultBathrooms);
        return Ok(item);
    }

    // PUT /api/unit-types/{id}
    [HttpPut("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUnitTypeRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { error = "Name is required." });

        try { await unitTypes.UpdateAsync(id, req.Name, req.Description,
            req.DefaultMonthlyLevy, req.DefaultBedrooms, req.DefaultBathrooms); }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
        return NoContent();
    }

    // PATCH /api/unit-types/{id}/toggle
    [HttpPatch("{id:guid}/toggle")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager")]
    public async Task<IActionResult> Toggle(Guid id)
    {
        try { await unitTypes.ToggleActiveAsync(id); }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
        return NoContent();
    }

    // DELETE /api/unit-types/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try { await unitTypes.DeleteAsync(id); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        return NoContent();
    }
}
