namespace FacilityApp.Middleware;

using FacilityApp.Services;

/// <summary>
/// Resolves a tenant from the request hostname when a custom domain is configured
/// (e.g. greatwallgardens.estate). Sets TenantContext so every downstream component
/// and page can skip their own slug-based resolution.
/// </summary>
public class TenantDomainMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantService tenantSvc, TenantContext tenantCtx)
    {
        if (!tenantCtx.IsResolved)
        {
            var host = context.Request.Host.Host.ToLower();
            var path = context.Request.Path.Value ?? "";

            // Skip local development hosts and platform/superadmin routes —
            // superadmin pages must never run under a tenant context, regardless
            // of which domain is used to reach them.
            bool isLocal       = host == "localhost" || host == "127.0.0.1" || host.StartsWith("172.16.");
            bool isSuperAdmin  = path.StartsWith("/superadmin", StringComparison.OrdinalIgnoreCase)
                              || path.StartsWith("/platform",   StringComparison.OrdinalIgnoreCase);

            if (!isLocal && !isSuperAdmin)
            {
                // 1. Try resolving by custom domain hostname (Caddy passes original host in X-Forwarded-Host)
                var forwardedHost = context.Request.Headers["X-Forwarded-Host"].FirstOrDefault()?.Split(',')[0].Trim();
                var resolveHost   = forwardedHost ?? host;

                var tenantByDomain = await tenantSvc.ResolveByDomainAsync(resolveHost);
                if (tenantByDomain is not null)
                {
                    tenantCtx.SetFromTenant(tenantByDomain, isCustomDomain: true);
                }
                else
                {
                    // 2. Fall back to X-Tenant-Slug header (sent by the Next.js axiosInstance on every request)
                    var slugHeader = context.Request.Headers["X-Tenant-Slug"].FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(slugHeader))
                    {
                        var tenantBySlug = await tenantSvc.ResolveBySlugAsync(slugHeader);
                        if (tenantBySlug is not null)
                            tenantCtx.SetFromTenant(tenantBySlug, isCustomDomain: false);
                    }
                }
            }
        }

        await next(context);
    }
}
