using System.Security.Claims;
using FacilityApp.Data.Models;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/incidents")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
    Roles = "Security,Receptionist,Manager,Admin")]
public class IncidentsController : ControllerBase
{
    private readonly IIncidentService _incidents;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    public IncidentsController(IIncidentService incidents) => _incidents = incidents;

    // GET /api/incidents
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _incidents.GetAllAsync();
        return Ok(items);
    }

    // GET /api/incidents/open-count
    [HttpGet("open-count")]
    public async Task<IActionResult> OpenCount()
    {
        var count = await _incidents.GetOpenCountAsync();
        return Ok(new { count });
    }

    // POST /api/incidents
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateIncidentRequest req)
    {
        var item = await _incidents.CreateAsync(
            req.Title, req.Description, req.Location, req.InvolvedParties,
            (IncidentCategory)req.Category, (IncidentSeverity)req.Severity, UserId);
        return Ok(item);
    }

    // PATCH /api/incidents/{id}
    [HttpPatch("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Manager,Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateIncidentRequest req)
    {
        await _incidents.UpdateStatusAsync(id, (IncidentStatus)req.Status, req.ResolutionNotes, UserId);
        return NoContent();
    }

    // DELETE /api/incidents/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _incidents.DeleteAsync(id);
        return NoContent();
    }
}
