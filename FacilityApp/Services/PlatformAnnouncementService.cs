using FacilityApp.Data;
using FacilityApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Services;

public class PlatformAnnouncementService : IPlatformAnnouncementService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public PlatformAnnouncementService(IDbContextFactory<AppDbContext> factory)
        => _factory = factory;

    public async Task<List<PlatformAnnouncement>> GetAllAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.PlatformAnnouncements
            .OrderByDescending(a => a.PublishedAt)
            .ToListAsync();
    }

    public async Task<PlatformAnnouncement> CreateAsync(
        string title, string body,
        AnnouncementCategory category,
        PlatformAudienceFilter audience,
        DateTime? expiresAt,
        string createdById,
        string createdByName)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var a = new PlatformAnnouncement
        {
            Title         = title.Trim(),
            Body          = body.Trim(),
            Category      = category,
            Audience      = audience,
            ExpiresAt     = expiresAt,
            CreatedById   = createdById,
            CreatedByName = createdByName,
        };
        db.PlatformAnnouncements.Add(a);
        await db.SaveChangesAsync();
        return a;
    }

    public async Task ToggleActiveAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var a = await db.PlatformAnnouncements.FindAsync(id)
            ?? throw new InvalidOperationException("Announcement not found.");
        a.IsActive = !a.IsActive;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var a = await db.PlatformAnnouncements.FindAsync(id)
            ?? throw new InvalidOperationException("Announcement not found.");
        db.PlatformAnnouncements.Remove(a);
        await db.SaveChangesAsync();
    }
}
