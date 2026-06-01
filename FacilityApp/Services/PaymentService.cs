using FacilityApp.Data;
using FacilityApp.Data.Models;
using FacilityApp.Services.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FacilityApp.Services;

public class PaymentService : IPaymentService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly TenantContext                   _tenantCtx;
    private readonly IHttpClientFactory              _httpFactory;
    private readonly IMemoryCache                    _cache;
    private readonly ILoggerFactory                  _loggerFactory;

    public PaymentService(
        IDbContextFactory<AppDbContext> factory,
        TenantContext tenantCtx,
        IHttpClientFactory httpFactory,
        IMemoryCache cache,
        ILoggerFactory loggerFactory)
    {
        _factory       = factory;
        _tenantCtx     = tenantCtx;
        _httpFactory   = httpFactory;
        _cache         = cache;
        _loggerFactory = loggerFactory;
    }

    // ── M-Pesa STK Push ───────────────────────────────────────────────────────

    public async Task<(bool Success, Guid? PaymentId, string? CheckoutRequestId, string? Error)>
        InitiateMpesaAsync(
            string phone, decimal amount,
            string? residentId, Guid? unitId,
            PaymentPurpose purpose, string? reference,
            string callbackUrl)
    {
        if (!_tenantCtx.MpesaEnabled
            || string.IsNullOrWhiteSpace(_tenantCtx.MpesaShortCode)
            || string.IsNullOrWhiteSpace(_tenantCtx.MpesaConsumerKey)
            || string.IsNullOrWhiteSpace(_tenantCtx.MpesaConsumerSecret)
            || string.IsNullOrWhiteSpace(_tenantCtx.MpesaPasskey))
        {
            return (false, null, null, "M-Pesa is not configured for this facility.");
        }

        // Build account reference — unit number preferred, fallback to reference
        var accountRef = reference ?? "PAYMENT";
        if (unitId.HasValue)
        {
            await using var refCtx = await _factory.CreateDbContextAsync();
            var unit = await refCtx.Units.FindAsync(unitId.Value);
            if (unit != null) accountRef = unit.UnitNumber;
        }

        var provider = new MpesaProvider(
            _tenantCtx.MpesaConsumerKey!,
            _tenantCtx.MpesaConsumerSecret!,
            _tenantCtx.MpesaShortCode!,
            _tenantCtx.MpesaPasskey!,
            _tenantCtx.MpesaSandbox,
            _httpFactory, _cache,
            _loggerFactory.CreateLogger<MpesaProvider>());

        var result = await provider.StkPushAsync(
            phone, amount, accountRef, purpose.ToString(), callbackUrl);

        if (!result.Success)
            return (false, null, null, result.Error);

        // Persist pending payment
        await using var ctx = await _factory.CreateDbContextAsync();
        var payment = new Payment
        {
            TenantId         = _tenantCtx.TenantId,
            ResidentId       = residentId,
            UnitId           = unitId,
            Amount           = amount,
            Method           = PaymentMethod.Mpesa,
            Status           = PaymentStatus.Processing,
            Purpose          = purpose,
            PhoneNumber      = phone,
            CheckoutRequestId = result.CheckoutRequestId,
            MerchantRequestId = result.MerchantRequestId,
            Reference        = reference,
        };
        ctx.Payments.Add(payment);
        await ctx.SaveChangesAsync();

        return (true, payment.Id, result.CheckoutRequestId, null);
    }

    // ── M-Pesa callback ───────────────────────────────────────────────────────

    public async Task HandleMpesaCallbackAsync(
        Guid tenantId, string checkoutRequestId,
        int resultCode, string resultDesc,
        string? mpesaReceiptNo, DateTime? transactionDate)
    {
        // Use IgnoreQueryFilters so we can match by tenantId + CheckoutRequestId
        // without needing a fully-resolved TenantContext.
        await using var ctx = await _factory.CreateDbContextAsync();
        var payment = await ctx.Payments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p =>
                p.TenantId == tenantId &&
                p.CheckoutRequestId == checkoutRequestId);

        if (payment is null) return;

        payment.ResultDescription = resultDesc;
        payment.UpdatedAt         = DateTime.UtcNow;

        if (resultCode == 0)
        {
            payment.Status        = PaymentStatus.Completed;
            payment.MpesaReceiptNo = mpesaReceiptNo;
            payment.PaidAt        = transactionDate ?? DateTime.UtcNow;
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
        }

        await ctx.SaveChangesAsync();
    }

    // ── Manual payment ────────────────────────────────────────────────────────

    public async Task<Guid> RecordManualAsync(
        string? residentId, Guid? unitId, decimal amount,
        PaymentMethod method, PaymentPurpose purpose,
        string? reference, string? notes, string recordedById)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var payment = new Payment
        {
            TenantId    = _tenantCtx.TenantId,
            ResidentId  = residentId,
            UnitId      = unitId,
            Amount      = amount,
            Method      = method,
            Status      = PaymentStatus.Completed,
            Purpose     = purpose,
            Reference   = reference,
            Notes       = notes,
            RecordedById = recordedById,
            PaidAt      = DateTime.UtcNow,
        };
        ctx.Payments.Add(payment);
        await ctx.SaveChangesAsync();
        return payment.Id;
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<(List<PaymentListDto> Items, int Total)> GetPagedAsync(
        int page, int pageSize, PaymentStatus? status = null)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var q = ctx.Payments
            .Include(p => p.Resident)
            .Include(p => p.Unit)
            .AsQueryable();

        if (status.HasValue)
            q = q.Where(p => p.Status == status.Value);

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => ToDto(p))
            .ToListAsync();

        return (items, total);
    }

    public async Task<PaymentListDto?> GetByIdAsync(Guid id)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var p = await ctx.Payments
            .Include(p => p.Resident)
            .Include(p => p.Unit)
            .FirstOrDefaultAsync(p => p.Id == id);
        return p is null ? null : ToDto(p);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PaymentListDto ToDto(Payment p) => new(
        p.Id,
        p.ResidentId,
        p.Resident != null ? $"{p.Resident.FirstName} {p.Resident.LastName}".Trim() : null,
        p.Unit?.UnitNumber,
        p.Amount, p.Currency,
        p.Method.ToString(),
        p.Status.ToString(),
        p.Purpose.ToString(),
        p.PhoneNumber,
        p.MpesaReceiptNo,
        p.Reference,
        p.CreatedAt,
        p.PaidAt);
}
