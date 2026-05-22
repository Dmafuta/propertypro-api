using FacilityApp.Data;
using FacilityApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Services;

public class EntranceService : IEntranceService
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenantCtx;

    public EntranceService(AppDbContext db, TenantContext tenantCtx)
    {
        _db        = db;
        _tenantCtx = tenantCtx;
    }

    public async Task<List<Entrance>> GetActiveAsync()
        => await _db.Entrances.Where(e => e.IsActive).OrderBy(e => e.Name).ToListAsync();

    public async Task<List<Entrance>> GetAllAsync()
        => await _db.Entrances.OrderBy(e => e.Name).ToListAsync();

    public async Task<Entrance?> GetByIdAsync(Guid id)
        => await _db.Entrances.FirstOrDefaultAsync(e => e.Id == id);

    public async Task<Entrance> CreateAsync(string name, string? description)
    {
        var entrance = new Entrance
        {
            TenantId    = _tenantCtx.TenantId,
            Name        = name.Trim(),
            Description = description?.Trim(),
            IsActive    = true
        };
        _db.Entrances.Add(entrance);
        await _db.SaveChangesAsync();
        return entrance;
    }

    public async Task UpdateAsync(Guid id, string name, string? description)
    {
        var entrance = await _db.Entrances.FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new InvalidOperationException("Entrance not found.");
        entrance.Name        = name.Trim();
        entrance.Description = description?.Trim();
        await _db.SaveChangesAsync();
    }

    public async Task ToggleActiveAsync(Guid id)
    {
        var entrance = await _db.Entrances.FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new InvalidOperationException("Entrance not found.");
        entrance.IsActive = !entrance.IsActive;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entrance = await _db.Entrances.FirstOrDefaultAsync(e => e.Id == id)
            ?? throw new InvalidOperationException("Entrance not found.");
        _db.Entrances.Remove(entrance);
        await _db.SaveChangesAsync();
    }

    public async Task SetCurrentEntranceAsync(string userId, Guid entranceId)
    {
        var user = await _db.Users.IgnoreQueryFilters()
                              .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new InvalidOperationException("User not found.");

        // IgnoreQueryFilters() bypasses tenant isolation, so we must verify ownership explicitly
        if (user.TenantId != _tenantCtx.TenantId)
            throw new InvalidOperationException("User does not belong to the current tenant.");

        user.CurrentEntranceId = entranceId;
        await _db.SaveChangesAsync();
    }
}
