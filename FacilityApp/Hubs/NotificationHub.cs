using FacilityApp.Data.Models;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace FacilityApp.Hubs;

/// <summary>
/// Clients join a group named after their tenantSlug.
/// Server broadcasts "VisitorCheckedIn" to that group on every check-in.
/// Requires authentication; JoinTenant validates the caller belongs to the requested tenant.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITenantService _tenantService;

    public NotificationHub(UserManager<ApplicationUser> userManager, ITenantService tenantService)
    {
        _userManager   = userManager;
        _tenantService = tenantService;
    }

    public async Task JoinTenant(string tenantSlug)
    {
        // SuperAdmin may monitor any tenant's feed
        if (Context.User!.IsInRole("SuperAdmin"))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, tenantSlug);
            return;
        }

        var user = await _userManager.GetUserAsync(Context.User);
        if (user is null) return;

        // Resolve the requested tenant and verify the caller belongs to it
        var tenant = await _tenantService.ResolveBySlugAsync(tenantSlug);
        if (tenant is null || tenant.Id != user.TenantId) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, tenantSlug);
    }

    public async Task LeaveTenant(string tenantSlug)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, tenantSlug);
    }
}
