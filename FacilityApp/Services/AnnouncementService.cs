using FacilityApp.Data;
using FacilityApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Services;

public class AnnouncementService : IAnnouncementService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly TenantContext _tenantCtx;

    public AnnouncementService(IDbContextFactory<AppDbContext> factory, TenantContext tenantCtx)
    {
        _factory   = factory;
        _tenantCtx = tenantCtx;
    }

    public async Task<List<Announcement>> GetActiveAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        return await db.Announcements
            .Include(a => a.CreatedBy)
            .Where(a => a.IsActive && (a.ExpiresAt == null || a.ExpiresAt > now))
            .OrderByDescending(a => a.PublishedAt)
            .ToListAsync();
    }

    public async Task<List<Announcement>> GetAllAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Announcements
            .Include(a => a.CreatedBy)
            .OrderByDescending(a => a.PublishedAt)
            .ToListAsync();
    }

    public async Task<Announcement> CreateAsync(string title, string body, AnnouncementCategory category, DateTime? expiresAt, string createdById)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var announcement = new Announcement
        {
            TenantId    = _tenantCtx.TenantId,
            Title       = title.Trim(),
            Body        = body.Trim(),
            Category    = category,
            ExpiresAt   = expiresAt,
            CreatedById = createdById
        };
        db.Announcements.Add(announcement);
        await db.SaveChangesAsync();
        return announcement;
    }

    public async Task ToggleActiveAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var a = await db.Announcements.FindAsync(id)
            ?? throw new InvalidOperationException("Announcement not found.");
        a.IsActive = !a.IsActive;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var a = await db.Announcements.FindAsync(id)
            ?? throw new InvalidOperationException("Announcement not found.");
        db.Announcements.Remove(a);
        await db.SaveChangesAsync();
    }
}
