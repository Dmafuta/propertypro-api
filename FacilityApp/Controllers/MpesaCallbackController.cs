using FacilityApp.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FacilityApp.Controllers;

/// <summary>
/// Public endpoint that receives Safaricom STK Push callbacks.
/// URL: POST /api/mpesa/callback/{slug}
/// No authentication — Safaricom calls this directly.
/// </summary>
[ApiController]
[Route("api/mpesa/callback")]
public class MpesaCallbackController : ControllerBase
{
    private readonly IPaymentService _payments;
    private readonly TenantService   _tenantSvc;
    private readonly TenantContext   _tenantCtx;

    public MpesaCallbackController(
        IPaymentService payments,
        TenantService tenantSvc,
        TenantContext tenantCtx)
    {
        _payments  = payments;
        _tenantSvc = tenantSvc;
        _tenantCtx = tenantCtx;
    }

    [HttpPost("{slug}")]
    public async Task<IActionResult> Receive(string slug)
    {
        // Resolve tenant from slug (no JWT in this request)
        var tenant = await _tenantSvc.ResolveBySlugAsync(slug);
        if (tenant is null) return NotFound();
        _tenantCtx.SetFromTenant(tenant);

        // Parse Safaricom callback body
        using var doc = await JsonDocument.ParseAsync(Request.Body);
        var root = doc.RootElement;

        if (!root.TryGetProperty("Body", out var body) ||
            !body.TryGetProperty("stkCallback", out var cb))
            return BadRequest();

        var checkoutId  = cb.TryGetProperty("CheckoutRequestID", out var ci) ? ci.GetString() : null;
        var resultCode  = cb.TryGetProperty("ResultCode",        out var rc) ? rc.GetInt32()  : -1;
        var resultDesc  = cb.TryGetProperty("ResultDesc",        out var rd) ? rd.GetString() ?? "" : "";

        if (checkoutId is null) return BadRequest();

        string?   receiptNo = null;
        DateTime? paidAt    = null;

        if (resultCode == 0 && cb.TryGetProperty("CallbackMetadata", out var meta) &&
            meta.TryGetProperty("Item", out var items))
        {
            foreach (var item in items.EnumerateArray())
            {
                var name = item.TryGetProperty("Name", out var n) ? n.GetString() : null;
                if (name == "MpesaReceiptNumber" && item.TryGetProperty("Value", out var v))
                    receiptNo = v.GetString();
                if (name == "TransactionDate" && item.TryGetProperty("Value", out var td))
                {
                    var raw = td.ToString();
                    if (DateTime.TryParseExact(raw, "yyyyMMddHHmmss",
                        null, System.Globalization.DateTimeStyles.None, out var dt))
                        paidAt = dt;
                }
            }
        }

        await _payments.HandleMpesaCallbackAsync(
            tenant.Id, checkoutId, resultCode, resultDesc, receiptNo, paidAt);

        // Safaricom expects a 200 OK with specific JSON to acknowledge receipt
        return Ok(new { ResultCode = 0, ResultDesc = "Accepted" });
    }
}
