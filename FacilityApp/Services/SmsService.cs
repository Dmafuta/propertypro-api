using FacilityApp.Services.Sms;
using Microsoft.Extensions.Logging;

namespace FacilityApp.Services;

/// <summary>
/// High-level SMS service — composes message templates and delegates sending
/// to the per-tenant provider resolved by SmsProviderFactory.
/// </summary>
public class SmsService : ISmsService
{
    private readonly SmsProviderFactory  _factory;
    private readonly TenantContext       _tenantCtx;
    private readonly ILogger<SmsService> _logger;

    public SmsService(SmsProviderFactory factory, TenantContext tenantCtx, ILogger<SmsService> logger)
    {
        _factory   = factory;
        _tenantCtx = tenantCtx;
        _logger    = logger;
    }

    public async Task SendAsync(string to, string message)
    {
        if (!_tenantCtx.SmsEnabled)
        {
            _logger.LogDebug("SMS disabled for tenant {Slug}. Skipping SMS to {To}", _tenantCtx.TenantSlug, to);
            return;
        }

        var provider = _factory.Create();
        await provider.SendAsync(to, message);
    }

    public Task SendVisitConfirmationAsync(string to, string hostName, string visitorName,
        string purpose, DateTime scheduledAt, string tenantName)
    {
        var when = scheduledAt.ToLocalTime().ToString("ddd d MMM 'at' h:mm tt");
        var msg  = $"{tenantName}: Hi {hostName}, {visitorName} is scheduled to visit you on {when}. Purpose: {purpose}.";
        return SendAsync(to, msg);
    }

    public Task SendCheckInAlertAsync(string to, string hostName, string visitorName,
        string purpose, string tenantName)
    {
        var msg = $"{tenantName}: Hi {hostName}, your visitor {visitorName} has just checked in. Purpose: {purpose}.";
        return SendAsync(to, msg);
    }

    public Task SendParcelArrivedAsync(string to, string recipientName, string description, string tenantName)
    {
        var msg = $"{tenantName}: Hi {recipientName}, a parcel has arrived for you — {description}. Please collect it from reception.";
        return SendAsync(to, msg);
    }

    public Task SendMaintenanceUpdateAsync(string to, string residentName, string title, string status, string tenantName)
    {
        var msg = $"{tenantName}: Hi {residentName}, your maintenance request \"{title}\" has been updated to {status}.";
        return SendAsync(to, msg);
    }

    public Task SendUnitRequestResultAsync(string to, string residentName, string unitNumber, bool approved, string tenantName)
    {
        var msg = approved
            ? $"{tenantName}: Hi {residentName}, your unit request for {unitNumber} has been approved. Welcome home!"
            : $"{tenantName}: Hi {residentName}, your unit request for {unitNumber} was not approved. Please contact management for details.";
        return SendAsync(to, msg);
    }
}
