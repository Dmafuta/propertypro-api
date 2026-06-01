using FacilityApp.Data.Models;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
           Roles = "Admin,Manager,Receptionist")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;
    private readonly TenantContext   _tenantCtx;

    public PaymentsController(IPaymentService payments, TenantContext tenantCtx)
    {
        _payments  = payments;
        _tenantCtx = tenantCtx;
    }

    // GET /api/payments?page=1&pageSize=25&status=
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] int? status = null)
    {
        var statusEnum = status.HasValue ? (PaymentStatus?)status.Value : null;
        var (items, total) = await _payments.GetPagedAsync(page, pageSize, statusEnum);
        return Ok(new { items, total, page, pageSize });
    }

    // GET /api/payments/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var dto = await _payments.GetByIdAsync(id);
        return dto is null ? NotFound() : Ok(dto);
    }

    // POST /api/payments/mpesa/stk-push
    [HttpPost("mpesa/stk-push")]
    public async Task<IActionResult> StkPush([FromBody] InitiateMpesaRequest req)
    {
        // Build callback URL from request context
        var baseUrl  = $"{Request.Scheme}://{Request.Host}";
        var callback = $"{baseUrl}/api/mpesa/callback/{_tenantCtx.TenantSlug}";

        var (ok, paymentId, checkoutId, err) = await _payments.InitiateMpesaAsync(
            req.Phone, req.Amount,
            req.ResidentId, req.UnitId,
            (PaymentPurpose)req.Purpose, req.Reference,
            callback);

        if (!ok) return BadRequest(new { error = err });

        return Ok(new { paymentId, checkoutId,
            message = "STK push sent. Ask the customer to check their phone." });
    }

    // POST /api/payments/manual
    [HttpPost("manual")]
    public async Task<IActionResult> RecordManual([FromBody] RecordManualPaymentRequest req)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var id = await _payments.RecordManualAsync(
            req.ResidentId, req.UnitId, req.Amount,
            (PaymentMethod)req.Method, (PaymentPurpose)req.Purpose,
            req.Reference, req.Notes, userId);
        return Ok(new { id });
    }
}
