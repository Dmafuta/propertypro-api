using System.Security.Claims;
using FacilityApp.Data.Models;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/meters")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class MetersController(IMeterService meters) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    // GET /api/meters/unit/{unitId}
    [HttpGet("unit/{unitId:guid}")]
    public async Task<IActionResult> GetForUnit(Guid unitId)
    {
        var items = await meters.GetForUnitAsync(unitId);
        return Ok(items.Select(x => new
        {
            x.Meter.Id, x.Meter.MeterNumber, x.Meter.SerialNumber,
            UtilityType  = x.Meter.UtilityType.ToString(),
            UtilityValue = (int)x.Meter.UtilityType,
            MeterMode    = x.Meter.MeterMode.ToString(),
            MeterModeValue = (int)x.Meter.MeterMode,
            x.Meter.Location, x.Meter.UnitOfMeasure, x.Meter.Metadata,
            x.Meter.IsActive, x.Meter.InstallDate, x.Meter.RetiredAt,
            x.Meter.Notes, x.Meter.PreviousMeterId, x.Meter.ReplacedByMeterId,
            LatestReading = x.LatestReading is null ? null : new
            {
                x.LatestReading.ReadingValue,
                x.LatestReading.ReadingDate,
                ReadingType = x.LatestReading.ReadingType.ToString(),
            },
            x.ReadingCount,
        }));
    }

    // GET /api/meters/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var meter = await meters.GetByIdAsync(id);
        if (meter is null) return NotFound();
        return Ok(new
        {
            meter.Id, meter.UnitId, meter.MeterNumber, meter.SerialNumber,
            UtilityType  = meter.UtilityType.ToString(),
            UtilityValue = (int)meter.UtilityType,
            MeterMode    = meter.MeterMode.ToString(),
            MeterModeValue = (int)meter.MeterMode,
            meter.Location, meter.UnitOfMeasure, meter.Metadata,
            meter.IsActive, meter.InstallDate, meter.RetiredAt, meter.Notes,
            meter.PreviousMeterId, meter.ReplacedByMeterId,
            Unit = new { meter.Unit.UnitNumber, meter.Unit.Block },
        });
    }

    // POST /api/meters
    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager")]
    public async Task<IActionResult> Add([FromBody] AddMeterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.MeterNumber))
            return BadRequest(new { error = "Meter number is required." });

        var meter = await meters.AddAsync(
            req.UnitId, (UtilityType)req.UtilityType, (MeterMode)req.MeterMode,
            req.MeterNumber, req.SerialNumber, req.Location, req.UnitOfMeasure,
            req.Metadata, req.Notes, req.InstallDate);
        return Ok(new { meter.Id });
    }

    // PUT /api/meters/{id}
    [HttpPut("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMeterRequest req)
    {
        try { await meters.UpdateAsync(id, req.MeterNumber, req.SerialNumber,
            req.Location, req.UnitOfMeasure, req.Metadata, req.Notes); }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
        return NoContent();
    }

    // POST /api/meters/{id}/retire
    [HttpPost("{id:guid}/retire")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager")]
    public async Task<IActionResult> Retire(Guid id, [FromBody] RetireMeterRequest req)
    {
        try { await meters.RetireAsync(id, req.ClosingReadingValue, req.RetiredAt, UserId, req.Notes); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        return NoContent();
    }

    // ── Readings ──────────────────────────────────────────────────────────────

    // GET /api/meters/{id}/readings?page=1&pageSize=20
    [HttpGet("{id:guid}/readings")]
    public async Task<IActionResult> GetReadings(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var items = await meters.GetReadingsAsync(id, page, pageSize);
        return Ok(items.Select(r => new
        {
            r.Id, r.ReadingValue, r.ReadingDate,
            ReadingType = r.ReadingType.ToString(),
            ReadBy = r.ReadBy?.FullName,
            r.PhotoUrl, r.Notes, r.IsVerified, r.CreatedAt,
        }));
    }

    // POST /api/meters/{id}/readings
    [HttpPost("{id:guid}/readings")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager,Security,Receptionist")]
    public async Task<IActionResult> AddReading(Guid id, [FromBody] AddReadingRequest req)
    {
        try
        {
            var reading = await meters.AddReadingAsync(id, req.Value, req.ReadingDate,
                (MeterReadingType)req.ReadingType, UserId, req.PhotoUrl, req.Notes);
            return Ok(new { reading.Id });
        }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
    }

    // PATCH /api/meters/readings/{readingId}/verify
    [HttpPatch("readings/{readingId:guid}/verify")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager")]
    public async Task<IActionResult> VerifyReading(Guid readingId)
    {
        try { await meters.VerifyReadingAsync(readingId); }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
        return NoContent();
    }

    // ── Prepaid tokens ────────────────────────────────────────────────────────

    // GET /api/meters/{id}/tokens?page=1&pageSize=20
    [HttpGet("{id:guid}/tokens")]
    public async Task<IActionResult> GetTokens(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var items = await meters.GetTokensAsync(id, page, pageSize);
        return Ok(items.Select(t => new
        {
            t.Id, t.TokenCode, t.AmountPaid, t.UnitsLoaded,
            t.PurchasedAt, t.LoadedAt, t.VoucherReference, t.Notes,
            PurchasedBy = t.PurchasedBy?.FullName, t.CreatedAt,
        }));
    }

    // POST /api/meters/{id}/tokens
    [HttpPost("{id:guid}/tokens")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager,Receptionist")]
    public async Task<IActionResult> AddToken(Guid id, [FromBody] AddTokenRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.TokenCode))
            return BadRequest(new { error = "Token code is required." });
        try
        {
            var token = await meters.AddTokenAsync(id, req.TokenCode, req.AmountPaid,
                req.UnitsLoaded, req.PurchasedAt, req.LoadedAt,
                UserId, req.VoucherReference, req.Notes);
            return Ok(new { token.Id });
        }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
    }

    // ── Alerts ────────────────────────────────────────────────────────────────

    // GET /api/meters/{id}/alerts?unacknowledgedOnly=false
    [HttpGet("{id:guid}/alerts")]
    public async Task<IActionResult> GetAlerts(Guid id, [FromQuery] bool unacknowledgedOnly = false)
    {
        var items = await meters.GetAlertsAsync(id, unacknowledgedOnly);
        return Ok(items.Select(a => new
        {
            a.Id, AlertType = a.AlertType.ToString(), Severity = a.Severity.ToString(),
            a.Message, a.TriggeredAt, a.AcknowledgedAt,
            AcknowledgedBy = a.AcknowledgedBy?.FullName,
        }));
    }

    // POST /api/meters/{id}/alerts
    [HttpPost("{id:guid}/alerts")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateAlert(Guid id, [FromBody] CreateAlertRequest req)
    {
        try
        {
            var alert = await meters.CreateAlertAsync(id, (AlertType)req.AlertType,
                (AlertSeverity)req.Severity, req.Message, req.TriggeredAt);
            return Ok(new { alert.Id });
        }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
    }

    // PATCH /api/meters/alerts/{alertId}/acknowledge
    [HttpPatch("alerts/{alertId:guid}/acknowledge")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Manager")]
    public async Task<IActionResult> AcknowledgeAlert(Guid alertId)
    {
        try { await meters.AcknowledgeAlertAsync(alertId, UserId); }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
        return NoContent();
    }

    // ── Installation report ───────────────────────────────────────────────────

    // GET /api/meters/{id}/report
    [HttpGet("{id:guid}/report")]
    public async Task<IActionResult> GetInstallationReport(Guid id)
    {
        try
        {
            var data = await meters.GetInstallationReportDataAsync(id);
            return Ok(data);
        }
        catch (InvalidOperationException ex) { return NotFound(new { error = ex.Message }); }
    }
}
