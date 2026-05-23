using FacilityApp.Data;
using FacilityApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Services;

public class BlacklistService : IBlacklistService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly TenantContext _tenantCtx;
    private readonly IAuditService _audit;

    public BlacklistService(IDbContextFactory<AppDbContext> factory, TenantContext tenantCtx, IAuditService audit)
    {
        _factory   = factory;
        _tenantCtx = tenantCtx;
        _audit     = audit;
    }

    public async Task<BlacklistEntry> AddAsync(
        string fullName, string? email, string? phone,
        string reason, BlacklistType entryType,
        DateTime? expiresAt, string? notes,
        string addedByUserId, string addedByName)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var entry = new BlacklistEntry
        {
            TenantId      = _tenantCtx.TenantId,
            FullName      = fullName.Trim(),
            Email         = email?.Trim().ToLower(),
            Phone         = phone?.Trim(),
            Reason        = reason.Trim(),
            EntryType     = entryType,
            ExpiresAt     = expiresAt,
            Notes         = notes?.Trim(),
            AddedByUserId = addedByUserId,
            AddedByName   = addedByName,
            IsActive      = true
        };
        db.BlacklistEntries.Add(entry);
        await db.SaveChangesAsync();

        await _audit.LogAsync(
            entryType == BlacklistType.Blacklisted ? "Blacklist" : "Watchlist",
            "BlacklistEntry", entry.Id.ToString(),
            $"{fullName} added — reason: {reason}",
            addedByUserId, addedByName);

        return entry;
    }

    public async Task RemoveAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var entry = await db.BlacklistEntries.FindAsync(id)
            ?? throw new InvalidOperationException("Entry not found.");
        entry.IsActive = false;
        await db.SaveChangesAsync();

        await _audit.LogAsync("RemoveFromList", "BlacklistEntry", id.ToString(),
            $"{entry.FullName} removed from {entry.EntryType}");
    }

    public async Task<(List<BlacklistEntry> Items, int Total)> GetEntriesAsync(
        string? search, string? type, int page = 1, int pageSize = 25)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var q = db.BlacklistEntries.Where(e => e.IsActive).AsQueryable();

        if (type == "blacklisted")
            q = q.Where(e => e.EntryType == BlacklistType.Blacklisted);
        else if (type == "watchlisted")
            q = q.Where(e => e.EntryType == BlacklistType.Watchlisted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(e =>
                e.FullName.ToLower().Contains(s) ||
                (e.Email != null && e.Email.Contains(s)) ||
                (e.Phone != null && e.Phone.Contains(s)) ||
                e.Reason.ToLower().Contains(s));
        }

        var total = await q.CountAsync();
        var items = await q
            .OrderByDescending(e => e.AddedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<BlacklistEntry?> CheckAsync(string? email, string? phone, Guid? entranceId = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var now       = DateTime.UtcNow;
        var emailNorm = email?.Trim().ToLower();

        return await db.BlacklistEntries
            .Where(e => e.IsActive &&
                        (e.ExpiresAt == null || e.ExpiresAt > now) &&
                        (e.EntranceId == null || (entranceId != null && e.EntranceId == entranceId)) &&
                        ((emailNorm != null && e.Email == emailNorm) ||
                         (phone != null && e.Phone == phone.Trim())))
            .OrderBy(e => e.EntryType)
            .FirstOrDefaultAsync();
    }
}
