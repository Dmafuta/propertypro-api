using FacilityApp.Data;
using FacilityApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Services;

public class AccessPassService : IAccessPassService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly TenantContext _tenantCtx;
    private readonly IAuditService _audit;

    public AccessPassService(IDbContextFactory<AppDbContext> factory, TenantContext tenantCtx, IAuditService audit)
    {
        _factory   = factory;
        _tenantCtx = tenantCtx;
        _audit     = audit;
    }

    public async Task<AccessPass> GenerateAsync(Guid visitId, PassType passType,
        string? vehicleRegistration, string? parkingBay, DateTime? validUntil)
    {
        await using var db = await _factory.CreateDbContextAsync();
        _ = await db.Visits.FindAsync(visitId)
            ?? throw new InvalidOperationException("Visit not found.");

        var pass = new AccessPass
        {
            TenantId            = _tenantCtx.TenantId,
            VisitId             = visitId,
            PassType            = passType,
            PassNumber          = GeneratePassNumber(),
            VehicleRegistration = vehicleRegistration?.Trim(),
            ParkingBay          = parkingBay?.Trim(),
            ValidFrom           = DateTime.UtcNow,
            ValidUntil          = validUntil
        };
        db.AccessPasses.Add(pass);
        await db.SaveChangesAsync();

        await _audit.LogAsync("GeneratePass", "AccessPass", pass.Id.ToString(),
            $"Pass {pass.PassNumber} issued — type: {passType}");

        return pass;
    }

    public async Task RevokeAsync(Guid passId, string? reason)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var pass = await db.AccessPasses.FindAsync(passId)
            ?? throw new InvalidOperationException("Pass not found.");

        pass.IsRevoked     = true;
        pass.RevokedAt     = DateTime.UtcNow;
        pass.RevokedReason = reason?.Trim();
        await db.SaveChangesAsync();

        await _audit.LogAsync("RevokePass", "AccessPass", passId.ToString(),
            $"Pass {pass.PassNumber} revoked — reason: {reason}");
    }

    public async Task<(List<AccessPass> Items, int Total)> GetPassesAsync(
        string? status, string? search, int page = 1, int pageSize = 25)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var q = db.AccessPasses
            .Include(p => p.Visit).ThenInclude(v => v.Visitor)
            .Include(p => p.Entrance)
            .AsQueryable();

        q = status switch
        {
            "active"  => q.Where(p => !p.IsRevoked && (p.ValidUntil == null || p.ValidUntil > DateTime.UtcNow)),
            "revoked" => q.Where(p => p.IsRevoked),
            "expired" => q.Where(p => !p.IsRevoked && p.ValidUntil != null && p.ValidUntil <= DateTime.UtcNow),
            _         => q
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(p =>
                p.PassNumber.ToLower().Contains(s) ||
                p.Visit.Visitor.FullName.ToLower().Contains(s) ||
                (p.VehicleRegistration != null && p.VehicleRegistration.ToLower().Contains(s)));
        }

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<AccessPass?> GetForVisitAsync(Guid visitId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.AccessPasses
            .Include(p => p.Visit).ThenInclude(v => v.Visitor)
            .Where(p => p.VisitId == visitId && !p.IsRevoked)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();
    }

    private static string GeneratePassNumber()
    {
        var year = DateTime.UtcNow.Year;
        var rand = Random.Shared.Next(100000, 999999);
        return $"PASS-{year}-{rand}";
    }
}
