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
    }
}
