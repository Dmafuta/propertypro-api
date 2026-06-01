using FacilityApp.Data;
using FacilityApp.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FacilityApp.Services;

public class PermissionService : IPermissionService
{
    private const string CachePrefix     = "perms:";
    private const string InvalidationKey = "perms:__version";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly UserManager<ApplicationUser> _users;
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IMemoryCache _cache;

    public PermissionService(
        UserManager<ApplicationUser> users,
        IDbContextFactory<AppDbContext> factory,
        IMemoryCache cache)
    {
        _users   = users;
        _factory = factory;
        _cache   = cache;
    }

    public async Task<HashSet<Permission>> GetPermissionsAsync(string userId)
    {
        var cacheKey     = $"{CachePrefix}{userId}";
        var globalVersion = _cache.Get<long>(InvalidationKey);

        if (_cache.TryGetValue(cacheKey, out (HashSet<Permission> Perms, long Version) entry)
            && entry.Version == globalVersion)
        {
            return entry.Perms;
        }

        var user = await _users.FindByIdAsync(userId);
        if (user is null) return [];

        var roles = await _users.GetRolesAsync(user);
        if (roles.Count == 0) return [];

        await using var ctx = await _factory.CreateDbContextAsync();

        var permissions = await (
            from ar in ctx.AppRoles.IgnoreQueryFilters()
            join rp in ctx.RolePermissions.IgnoreQueryFilters() on ar.Id equals rp.AppRoleId
            where roles.Contains(ar.Name) && ar.IsActive
            select rp.Permission)
            .Distinct()
            .ToListAsync();

        var result = permissions.ToHashSet();
        _cache.Set(cacheKey, (result, globalVersion), CacheTtl);
        return result;
    }

    public void InvalidateUser(string userId) =>
        _cache.Remove($"{CachePrefix}{userId}");

    /// <summary>
    /// Bumps the global version counter, effectively invalidating all cached
    /// permission sets within one TTL cycle.
    /// </summary>
    public void InvalidateAll() =>
        _cache.Set(InvalidationKey, (_cache.Get<long>(InvalidationKey)) + 1,
            DateTimeOffset.MaxValue);
}
