using FacilityApp.Data.Models;
using Microsoft.Extensions.Logging;

namespace FacilityApp.Services.Sms;

/// <summary>
/// Resolves the correct ISmsProvider for the current tenant at runtime.
/// Falls back to the platform Africa's Talking config when no tenant credentials are set.
/// </summary>
public class SmsProviderFactory
{
    private readonly TenantContext              _tenantCtx;
    private readonly AfricasTalkingSettings     _platformAt;
    private readonly IHttpClientFactory         _httpFactory;
    private readonly ILoggerFactory             _loggerFactory;

    public SmsProviderFactory(
        TenantContext tenantCtx,
        AfricasTalkingSettings platformAt,
        IHttpClientFactory httpFactory,
        ILoggerFactory loggerFactory)
    {
        _tenantCtx     = tenantCtx;
        _platformAt    = platformAt;
        _httpFactory   = httpFactory;
        _loggerFactory = loggerFactory;
    }

    public ISmsProvider Create()
    {
        // Professional tenants with their own credentials use their chosen provider.
        // Starter tenants (or Professional with no key set) fall back to the platform AT account.
        var hasTenantCreds = _tenantCtx.Plan == TenantPlan.Professional
                          && !string.IsNullOrWhiteSpace(_tenantCtx.SmsApiKey);

        if (!hasTenantCreds)
            return BuildAfricasTalking(
                _platformAt.ApiKey, _platformAt.Username, _platformAt.SenderId, _platformAt.Sandbox);

        return _tenantCtx.SmsProvider switch
        {
            SmsProvider.Twilio => new TwilioProvider(
                accountSid: _tenantCtx.SmsUsername ?? "",
                authToken:  _tenantCtx.SmsApiKey!,
                from:       _tenantCtx.SmsSenderId ?? "",
                _httpFactory,
                _loggerFactory.CreateLogger<TwilioProvider>()),

            SmsProvider.Vonage => new VonageProvider(
                apiKey:    _tenantCtx.SmsApiKey!,
                apiSecret: _tenantCtx.SmsUsername ?? "",
                from:      _tenantCtx.SmsSenderId ?? "",
                _httpFactory,
                _loggerFactory.CreateLogger<VonageProvider>()),

            SmsProvider.CustomHttp => new CustomHttpProvider(
                apiUrl:  _tenantCtx.SmsApiUrl ?? "",
                apiKey:  _tenantCtx.SmsApiKey,
                from:    _tenantCtx.SmsSenderId,
                _httpFactory,
                _loggerFactory.CreateLogger<CustomHttpProvider>()),

            _ => BuildAfricasTalking(
                _tenantCtx.SmsApiKey!,
                _tenantCtx.SmsUsername ?? _tenantCtx.TenantSlug,
                _tenantCtx.SmsSenderId,
                sandbox: false),
        };
    }

    private AfricasTalkingProvider BuildAfricasTalking(
        string apiKey, string username, string? senderId, bool sandbox)
        => new(apiKey, username, senderId, sandbox,
               _httpFactory, _loggerFactory.CreateLogger<AfricasTalkingProvider>());
}
