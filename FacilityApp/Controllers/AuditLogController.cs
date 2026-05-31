using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/audit-log")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Manager,Admin")]
public class AuditLogController : ControllerBase
{
    private readonly IAuditService _audit;

    public AuditLogController(IAuditService audit) => _audit = audit;

    // GET /api/audit-log?search=x&page=1&pageSize=50
    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] string? search   = null,
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 50)
    {
        var (items, total) = await _audit.GetLogsAsync(search, page, pageSize);
        var dtos = items.Select(a => new AuditLogDto(
            a.Id, a.UserId, a.UserName, a.Action,
            a.EntityType, a.EntityId, a.Details, a.CreatedAt));
        return Ok(new { items = dtos, total, page, pageSize });
    }
}
