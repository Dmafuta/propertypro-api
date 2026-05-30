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
                var tenant = await tenantSvc.ResolveByDomainAsync(host);
                if (tenant is not null)
                    tenantCtx.SetFromTenant(tenant, isCustomDomain: true);
            }
        }

        await next(context);
    }
}
