using FacilityApp.Data;
using FacilityApp.Data.Models;
using FacilityApp.Services;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Tests;

/// <summary>
/// Verifies that EF Core global query filters enforce data isolation between tenants,
/// and that services with IgnoreQueryFilters() do their own ownership checks.
/// </summary>
public class TenantIsolationTests : IDisposable
{
    private readonly Guid _tenantAId = Guid.NewGuid();
    private readonly Guid _tenantBId = Guid.NewGuid();

    // Shared in-memory database name — both contexts point at the same data store
    private readonly string _dbName = Guid.NewGuid().ToString();

    public TenantIsolationTests()
    {
        // Seed audit logs for both tenants using a filter-less context
        using var ctx = BuildContext(_tenantAId);
        ctx.AuditLogs.AddRange(
            new AuditLog { TenantId = _tenantAId, Action = "CheckIn",  EntityType = "Visit", CreatedAt = DateTime.UtcNow },
            new AuditLog { TenantId = _tenantAId, Action = "CheckOut", EntityType = "Visit", CreatedAt = DateTime.UtcNow },
            new AuditLog { TenantId = _tenantBId, Action = "CheckIn",  EntityType = "Visit", CreatedAt = DateTime.UtcNow }
        );

        // Seed visitors for both tenants
        ctx.Visitors.AddRange(
            new Visitor { TenantId = _tenantAId, FullName = "Alice (A)", Email = "alice@a.com", CreatedAt = DateTime.UtcNow },
            new Visitor { TenantId = _tenantBId, FullName = "Bob (B)",   Email = "bob@b.com",   CreatedAt = DateTime.UtcNow }
        );

        ctx.SaveChanges();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private AppDbContext BuildContext(Guid tenantId)
    {
        var tenantCtx = new TenantContext
        {
            TenantId   = tenantId,
            TenantSlug = tenantId == _tenantAId ? "tenant-a" : "tenant-b",
            IsResolved = true
        };
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_dbName)
            .Options;
        return new AppDbContext(opts, tenantCtx);
    }

    // ── AuditLog isolation ────────────────────────────────────────────────────

    [Fact]
    public async Task AuditLogs_TenantA_CannotSeeTenantBRecords()
    {
        await using var ctx = BuildContext(_tenantAId);
        var svc = new AuditService(ctx, new TenantContext { TenantId = _tenantAId, IsResolved = true });

        var (logs, total) = await svc.GetLogsAsync(null);

        Assert.Equal(2, total);
        Assert.All(logs, l => Assert.Equal(_tenantAId, l.TenantId));
    }

    [Fact]
    public async Task AuditLogs_TenantB_CannotSeeTenantARecords()
    {
        await using var ctx = BuildContext(_tenantBId);
        var svc = new AuditService(ctx, new TenantContext { TenantId = _tenantBId, IsResolved = true });

        var (logs, total) = await svc.GetLogsAsync(null);

        Assert.Equal(1, total);
        Assert.All(logs, l => Assert.Equal(_tenantBId, l.TenantId));
    }

    // ── Visitor isolation ─────────────────────────────────────────────────────

    [Fact]
    public async Task Visitors_TenantA_CannotSeeTenantBVisitors()
    {
        await using var ctx = BuildContext(_tenantAId);
        var visitors = await ctx.Visitors.ToListAsync();

        Assert.Single(visitors);
        Assert.Equal("Alice (A)", visitors[0].FullName);
    }

    [Fact]
    public async Task Visitors_TenantB_CannotSeeTenantAVisitors()
    {
        await using var ctx = BuildContext(_tenantBId);
        var visitors = await ctx.Visitors.ToListAsync();

        Assert.Single(visitors);
        Assert.Equal("Bob (B)", visitors[0].FullName);
    }

    // ── EntranceService.SetCurrentEntranceAsync cross-tenant guard ────────────

    [Fact]
    public async Task SetCurrentEntrance_ThrowsWhenUserBelongsToDifferentTenant()
    {
        // Seed a user that belongs to tenant B (bypass query filters via IgnoreQueryFilters seed context)
        var crossTenantUserId = Guid.NewGuid().ToString();
        {
            await using var seedCtx = BuildContext(_tenantBId);
            seedCtx.Users.Add(new ApplicationUser
            {
                Id       = crossTenantUserId,
                TenantId = _tenantBId,
                UserName = "b-user@example.com",
                Email    = "b-user@example.com",
                FullName = "Tenant B User"
            });
            await seedCtx.SaveChangesAsync();
        }

        // EntranceService acting as tenant A tries to modify a tenant-B user
        await using var ctxA     = BuildContext(_tenantAId);
        var tenantAContext        = new TenantContext { TenantId = _tenantAId, IsResolved = true };
        var svc                   = new EntranceService(ctxA, tenantAContext);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.SetCurrentEntranceAsync(crossTenantUserId, Guid.NewGuid()));
    }

    [Fact]
    public async Task SetCurrentEntrance_SucceedsWhenUserBelongsToSameTenant()
    {
        var userId = Guid.NewGuid().ToString();
        {
            await using var seedCtx = BuildContext(_tenantAId);
            seedCtx.Users.Add(new ApplicationUser
            {
                Id       = userId,
                TenantId = _tenantAId,
                UserName = "a-user@example.com",
                Email    = "a-user@example.com",
                FullName = "Tenant A User"
            });
            await seedCtx.SaveChangesAsync();
        }

        await using var ctxA = BuildContext(_tenantAId);
        var tenantAContext    = new TenantContext { TenantId = _tenantAId, IsResolved = true };
        var svc               = new EntranceService(ctxA, tenantAContext);

        // Should not throw
        await svc.SetCurrentEntranceAsync(userId, Guid.NewGuid());

        await using var verify = BuildContext(_tenantAId);
        var user = await verify.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == userId);
        Assert.NotNull(user.CurrentEntranceId);
    }

    // ── TenantContext.Reset ───────────────────────────────────────────────────

    [Fact]
    public void TenantContext_Reset_ClearsAllState()
    {
        var ctx = new TenantContext
        {
            TenantId       = Guid.NewGuid(),
            TenantSlug     = "acme",
            TenantName     = "Acme Corp",
            IsResolved     = true,
            PrimaryColour  = "#ff0000",
            LogoUrl        = "/logo.png",
            Plan           = TenantPlan.Professional,
            IsSystem       = true,
            IsCustomDomain = true
        };

        ctx.Reset();

        Assert.Equal(Guid.Empty, ctx.TenantId);
        Assert.Equal(string.Empty, ctx.TenantSlug);
        Assert.Equal(string.Empty, ctx.TenantName);
        Assert.False(ctx.IsResolved);
        Assert.Null(ctx.PrimaryColour);
        Assert.Null(ctx.LogoUrl);
        Assert.Equal(TenantPlan.Starter, ctx.Plan);
        Assert.False(ctx.IsSystem);
        Assert.False(ctx.IsCustomDomain);
    }

    public void Dispose() { }
}
