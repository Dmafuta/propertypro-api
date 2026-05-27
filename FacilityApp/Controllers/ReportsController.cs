using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Manager,Admin")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports) => _reports = reports;

    // GET /api/reports/stats?from=2026-01-01&to=2026-01-31
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] string? from, [FromQuery] string? to)
    {
        var fromDate = DateOnly.TryParse(from, out var fd) ? fd : DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var toDate   = DateOnly.TryParse(to,   out var td) ? td : DateOnly.FromDateTime(DateTime.Today);
        var stats    = await _reports.GetStatsAsync(fromDate, toDate);
        return Ok(stats);
    }

    // GET /api/reports/export?from=2026-01-01&to=2026-01-31
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] string? from, [FromQuery] string? to)
    {
        var fromDate = DateOnly.TryParse(from, out var fd) ? fd : DateOnly.FromDateTime(DateTime.Today.AddDays(-30));
        var toDate   = DateOnly.TryParse(to,   out var td) ? td : DateOnly.FromDateTime(DateTime.Today);
        var bytes    = await _reports.GetCsvBytesAsync(fromDate, toDate);
        var filename = $"visits-{fromDate:yyyy-MM-dd}-to-{toDate:yyyy-MM-dd}.csv";
        return File(bytes, "text/csv; charset=utf-8", filename);
    }
}
