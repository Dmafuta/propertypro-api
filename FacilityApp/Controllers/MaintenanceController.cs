using System.Security.Claims;
using FacilityApp.Data.Models;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/maintenance")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class MaintenanceController : ControllerBase
{
    private readonly IMaintenanceService _maintenance;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    public MaintenanceController(IMaintenanceService maintenance) => _maintenance = maintenance;

    // GET /api/maintenance?status=0   (staff — all requests, optionally filtered)
    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Receptionist,Security,Manager,Admin")]
    public async Task<IActionResult> GetAll([FromQuery] int? status = null)
    {
        MaintenanceStatus? s = status.HasValue ? (MaintenanceStatus)status.Value : null;
        var items = await _maintenance.GetAllAsync(s);
        return Ok(items);
    }

    // GET /api/maintenance/my   (resident — own requests)
    [HttpGet("my")]
    public async Task<IActionResult> GetMy()
    {
        var items = await _maintenance.GetForResidentAsync(UserId);
        return Ok(items);
    }

    // GET /api/maintenance/open-count
    [HttpGet("open-count")]
    public async Task<IActionResult> OpenCount()
    {
        var count = await _maintenance.GetOpenCountAsync();
        return Ok(new { count });
    }

    // POST /api/maintenance
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitMaintenanceRequest req)
    {
        var item = await _maintenance.SubmitAsync(
            UserId, req.UnitId, req.Title, req.Description,
            (MaintenanceCategory)req.Category, (MaintenancePriority)req.Priority);
        return Ok(item);
    }

    // PATCH /api/maintenance/{id}
    [HttpPatch("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Receptionist,Security,Manager,Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateMaintenanceRequest req)
    {
        await _maintenance.UpdateStatusAsync(id, (MaintenanceStatus)req.Status, req.StaffNote);
        return NoContent();
    }
}
