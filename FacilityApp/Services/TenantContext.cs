using FacilityApp.Data.Models;

namespace FacilityApp.Services;

public class TenantContext
{
    public Guid TenantId { get; set; } = Guid.Empty;
    public string TenantSlug { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
    public string? PrimaryColour { get; set; }
    public string? LogoUrl { get; set; }
    public TenantPlan Plan { get; set; } = TenantPlan.Starter;
    public bool IsSystem { get; set; }

    /// <summary>True when the tenant was resolved from a custom hostname (e.g. greatwallgardens.estate).</summary>
    public bool IsCustomDomain { get; set; }

    // Per-tenant SMS settings
    public bool        SmsEnabled  { get; set; } = true;
    public SmsProvider SmsProvider { get; set; } = SmsProvider.AfricasTalking;
    public string?     SmsApiKey   { get; set; }
    public string?     SmsUsername { get; set; }
    public string?     SmsSenderId { get; set; }
    public string?     SmsApiUrl   { get; set; }

    // M-Pesa (Safaricom Daraja)
    public bool    MpesaEnabled        { get; set; } = false;
    public bool    MpesaSandbox        { get; set; } = true;
    public string? MpesaShortCode      { get; set; }
    public string? MpesaConsumerKey    { get; set; }
    public string? MpesaConsumerSecret { get; set; }
    public string? MpesaPasskey        { get; set; }

    /// <summary>
    /// URL prefix for generating links.
    /// Empty string on a custom domain, "/{slug}" on a shared domain.
    /// Use as: href="@(TenantCtx.RouteBase)/dashboard"
    /// </summary>
    public string RouteBase => IsCustomDomain ? "" : (string.IsNullOrEmpty(TenantSlug) ? "" : $"/{TenantSlug}");

    /// <summary>
    /// Clears all resolved state so the next component initialisation re-resolves
    /// from the URL. Called by MainLayout when the slug changes during soft navigation.
    /// </summary>
    /// <summary>
    /// Populates TenantContext from JWT claims for API requests.
    /// Called by the JWT tenant resolution middleware after authentication.
    /// </summary>
    public void SetFromJwt(Guid tenantId, string tenantSlug, string tenantName)
    {
        TenantId   = tenantId;
        TenantSlug = tenantSlug;
        TenantName = tenantName;
        IsResolved = true;
    }

    public void SetFromTenant(Data.Models.Tenant t, bool isCustomDomain = false)
    {
        TenantId       = t.Id;
        TenantSlug     = t.Slug;
        TenantName     = t.Name;
        PrimaryColour  = t.PrimaryColour;
        LogoUrl        = t.LogoUrl;
        Plan           = t.Plan;
        IsSystem       = t.IsSystem;
        IsCustomDomain = isCustomDomain;
        SmsEnabled     = t.SmsEnabled;
        SmsProvider    = t.SmsProvider;
        SmsApiKey      = t.SmsApiKey;
        SmsUsername    = t.SmsUsername;
        SmsSenderId    = t.SmsSenderId;
        SmsApiUrl      = t.SmsApiUrl;
        MpesaEnabled        = t.MpesaEnabled;
        MpesaSandbox        = t.MpesaSandbox;
        MpesaShortCode      = t.MpesaShortCode;
        MpesaConsumerKey    = t.MpesaConsumerKey;
        MpesaConsumerSecret = t.MpesaConsumerSecret;
        MpesaPasskey        = t.MpesaPasskey;
        IsResolved     = true;
    }

    public void Reset()
    {
        TenantId       = Guid.Empty;
        TenantSlug     = string.Empty;
        TenantName     = string.Empty;
        IsResolved     = false;
        PrimaryColour  = null;
        LogoUrl        = null;
        Plan           = TenantPlan.Starter;
        IsSystem       = false;
        IsCustomDomain = false;
        SmsEnabled     = true;
        SmsProvider    = SmsProvider.AfricasTalking;
        SmsApiKey      = null;
        SmsUsername    = null;
        SmsSenderId    = null;
        SmsApiUrl      = null;
        MpesaEnabled        = false;
        MpesaSandbox        = true;
        MpesaShortCode      = null;
        MpesaConsumerKey    = null;
        MpesaConsumerSecret = null;
        MpesaPasskey        = null;
    }
}
