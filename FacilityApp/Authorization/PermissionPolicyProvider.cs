using FacilityApp.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace FacilityApp.Authorization;

/// <summary>
/// Dynamically generates authorization policies for "Permission:{PermissionName}" keys.
/// All other policy names fall through to the default provider.
/// </summary>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    internal const string PolicyPrefix = "Permission:";

    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permissionName = policyName[PolicyPrefix.Length..];
            if (Enum.TryParse<Permission>(permissionName, out var permission))
            {
                return new AuthorizationPolicyBuilder()
                    .AddRequirements(new PermissionRequirement(permission))
                    .Build();
            }
        }

        return await _fallback.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallback.GetFallbackPolicyAsync();
}
