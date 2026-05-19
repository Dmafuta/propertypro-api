using FacilityApp.Data;
using FacilityApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Services;

public class AnnouncementService : IAnnouncementService
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenantCtx;

    public AnnouncementService(AppDbContext db, TenantContext tenantCtx)
    {
        _db        = db;
        _tenantCtx = tenantCtx;
    }

    public async Task<List<Announcement>> GetActiveAsync()
    {
        var now = DateTime.UtcNow;
        return await _db.Announcements
            .Include(a => a.CreatedBy)
            .Where(a => a.IsActive && (a.ExpiresAt == null || a.ExpiresAt > now))
            .OrderByDescending(a => a.PublishedAt)
            .ToListAsync();
    }

    public async Task<List<Announcement>> GetAllAsync()
    {
        return await _db.Announcements
            .Include(a => a.CreatedBy)
            .OrderByDescending(a => a.PublishedAt)
            .ToListAsync();
    }

    public async Task<Announcement> CreateAsync(string title, string body, AnnouncementCategory category, DateTime? expiresAt, string createdById)
    {
        var announcement = new Announcement
        {
            TenantId    = _tenantCtx.TenantId,
            Title       = title.Trim(),
            Body        = body.Trim(),
            Category    = category,
            ExpiresAt   = expiresAt,
            CreatedById = createdById
        };
        _db.Announcements.Add(announcement);
        await _db.SaveChangesAsync();
        return announcement;
    }

    public async Task ToggleActiveAsync(Guid id)
    {
        var a = await _db.Announcements.FindAsync(id)
            ?? throw new InvalidOperationException("Announcement not found.");
        a.IsActive = !a.IsActive;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var a = await _db.Announcements.FindAsync(id)
            ?? throw new InvalidOperationException("Announcement not found.");
        _db.Announcements.Remove(a);
        await _db.SaveChangesAsync();
    }
}
