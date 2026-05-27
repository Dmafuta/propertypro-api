using System.Security.Claims;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/visitors")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class VisitorsController : ControllerBase
{
    private readonly IVisitorService _visitors;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    public VisitorsController(IVisitorService visitors) => _visitors = visitors;

    // GET /api/visitors/visits?tab=today&search=&page=1
    [HttpGet("visits")]
    public async Task<IActionResult> GetVisits(
        [FromQuery] string tab = "today",
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var (items, total) = await _visitors.GetVisitsAsync(tab, search, page, pageSize);
        return Ok(new { items, total, page, pageSize });
    }

    // GET /api/visitors/visits/scheduled
    [HttpGet("visits/scheduled")]
    public async Task<IActionResult> GetScheduled([FromQuery] string? search = null)
    {
        var items = await _visitors.GetScheduledVisitsAsync(search);
        return Ok(items);
    }

    // GET /api/visitors/visits/my  (resident: own visits)
    [HttpGet("visits/my")]
    public async Task<IActionResult> GetMyVisits()
    {
        var items = await _visitors.GetVisitsForHostAsync(UserId);
        return Ok(items);
    }

    // GET /api/visitors/hosts
    [HttpGet("hosts")]
    public async Task<IActionResult> GetHosts()
    {
        var hosts = await _visitors.GetHostsAsync();
        return Ok(hosts.Select(h => new { h.Id, h.FullName, h.Email }));
    }

    // POST /api/visitors/walk-in
    [HttpPost("walk-in")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Security,Manager,Admin")]
    public async Task<IActionResult> WalkIn([FromBody] WalkInRequest req)
    {
        try
        {
            var visit = await _visitors.WalkInAsync(
                req.FullName, req.Email, req.Phone, req.Company,
                req.Purpose, req.HostUserId, req.Notes, null, req.EntranceId);
            return Ok(visit);
        }
        catch (VisitorBlockedException ex)
        {
            return Conflict(new { error = "Visitor is blocked.", reason = ex.Entry.Reason });
        }
    }

    // POST /api/visitors/pre-register
    [HttpPost("pre-register")]
    public async Task<IActionResult> PreRegister([FromBody] PreRegisterRequest req)
    {
        var visit = await _visitors.PreRegisterAsync(
            req.FullName, req.Email, req.Phone, req.Company,
            req.Purpose, req.HostUserId, req.ScheduledAt, req.Notes);
        return Ok(visit);
    }

    // PATCH /api/visitors/visits/{id}/check-in
    [HttpPatch("visits/{id:guid}/check-in")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Security,Manager,Admin")]
    public async Task<IActionResult> CheckIn(Guid id, [FromBody] CheckInRequest req)
    {
        try
        {
            await _visitors.CheckInAsync(id, req.EntranceId);
            return NoContent();
        }
        catch (VisitorBlockedException ex)
        {
            return Conflict(new { error = "Visitor is blocked.", reason = ex.Entry.Reason });
        }
    }

    // PATCH /api/visitors/visits/{id}/check-out
    [HttpPatch("visits/{id:guid}/check-out")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Security,Manager,Admin")]
    public async Task<IActionResult> CheckOut(Guid id, [FromBody] CheckOutRequest req)
    {
        await _visitors.CheckOutAsync(id, req.EntranceId);
        return NoContent();
    }

    // PATCH /api/visitors/visits/{id}/cancel
    [HttpPatch("visits/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await _visitors.CancelAsync(id);
        return NoContent();
    }

    // PATCH /api/visitors/visits/{id}/no-show
    [HttpPatch("visits/{id:guid}/no-show")]
    public async Task<IActionResult> NoShow(Guid id)
    {
        await _visitors.MarkNoShowAsync(id);
        return NoContent();
    }
}
