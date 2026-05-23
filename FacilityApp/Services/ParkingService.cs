using FacilityApp.Data;
using FacilityApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Services;

public class ParkingService : IParkingService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly TenantContext _tenantCtx;

    public ParkingService(IDbContextFactory<AppDbContext> factory, TenantContext tenantCtx)
    {
        _factory   = factory;
        _tenantCtx = tenantCtx;
    }

    // ── Vehicles ───────────────────────────────────────────────────────────────

    public async Task<List<Vehicle>> GetVehiclesForResidentAsync(string residentId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Vehicles
            .Include(v => v.Tag)
            .Where(v => v.OwnerId == residentId)
            .OrderBy(v => v.PlateNumber)
            .ToListAsync();
    }

    public async Task<List<Vehicle>> GetAllVehiclesAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Vehicles
            .Include(v => v.Owner)
            .Include(v => v.Tag)
            .OrderBy(v => v.Owner.FullName)
            .ThenBy(v => v.PlateNumber)
            .ToListAsync();
    }

    public async Task<Vehicle> RegisterVehicleAsync(string ownerId, string plate, string make, string model, string colour, VehicleType type, string? notes)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var vehicle = new Vehicle
        {
            TenantId    = _tenantCtx.TenantId,
            OwnerId     = ownerId,
            PlateNumber = plate.Trim().ToUpperInvariant(),
            Make        = make.Trim(),
            Model       = model.Trim(),
            Colour      = colour.Trim(),
            Type        = type,
            Notes       = notes?.Trim()
        };

        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        return vehicle;
    }

    public async Task DeleteVehicleAsync(Guid vehicleId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var vehicle = await db.Vehicles.FindAsync(vehicleId)
            ?? throw new InvalidOperationException("Vehicle not found.");
        db.Vehicles.Remove(vehicle);
        await db.SaveChangesAsync();
    }

    // ── Tags ───────────────────────────────────────────────────────────────────

    public async Task<VehicleTag> IssueTagAsync(Guid vehicleId, string issuedById, DateTime? expiresAt, string? notes)
    {
        await using var db = await _factory.CreateDbContextAsync();
        _ = await db.Vehicles.FindAsync(vehicleId)
            ?? throw new InvalidOperationException("Vehicle not found.");

        var existing = await db.VehicleTags.FirstOrDefaultAsync(t => t.VehicleId == vehicleId);
        if (existing is not null)
            throw new InvalidOperationException("This vehicle already has a tag. Revoke it first.");

        var tagNumber = await GenerateTagNumberAsync(db);

        var tag = new VehicleTag
        {
            TenantId   = _tenantCtx.TenantId,
            VehicleId  = vehicleId,
            TagNumber  = tagNumber,
            Status     = TagStatus.Active,
            IssuedById = issuedById,
            IssuedAt   = DateTime.UtcNow,
            ExpiresAt  = expiresAt,
            Notes      = notes?.Trim()
        };

        db.VehicleTags.Add(tag);
        await db.SaveChangesAsync();
        return tag;
    }

    public async Task UpdateTagStatusAsync(Guid tagId, TagStatus status)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var tag = await db.VehicleTags.FindAsync(tagId)
            ?? throw new InvalidOperationException("Tag not found.");
        tag.Status = status;
        await db.SaveChangesAsync();
    }

    public async Task<VehicleTag?> LookupTagAsync(string tagNumber)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.VehicleTags
            .Include(t => t.Vehicle)
                .ThenInclude(v => v.Owner)
            .FirstOrDefaultAsync(t => t.TagNumber == tagNumber.Trim().ToUpperInvariant());
    }

    // ── Parking records ────────────────────────────────────────────────────────

    public async Task<ParkingRecord> LogEntryByTagAsync(string tagNumber, string loggedById, Guid? entranceId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var tag = await db.VehicleTags
            .Include(t => t.Vehicle).ThenInclude(v => v.Owner)
            .FirstOrDefaultAsync(t => t.TagNumber == tagNumber.Trim().ToUpperInvariant())
            ?? throw new InvalidOperationException($"Tag '{tagNumber}' not found.");

        if (tag.Status == TagStatus.Revoked)
            throw new InvalidOperationException($"Tag {tag.TagNumber} is REVOKED. Entry denied.");
        if (tag.Status == TagStatus.Suspended)
            throw new InvalidOperationException($"Tag {tag.TagNumber} is SUSPENDED. Entry denied.");
        if (tag.Status == TagStatus.Expired || (tag.ExpiresAt.HasValue && tag.ExpiresAt.Value < DateTime.UtcNow))
            throw new InvalidOperationException($"Tag {tag.TagNumber} is EXPIRED. Entry denied.");

        var alreadyInside = await db.ParkingRecords
            .AnyAsync(p => p.VehicleTagId == tag.Id && p.ExitedAt == null);
        if (alreadyInside)
            throw new InvalidOperationException($"Vehicle with tag {tag.TagNumber} is already logged inside.");

        var record = new ParkingRecord
        {
            TenantId        = _tenantCtx.TenantId,
            VehicleId       = tag.VehicleId,
            VehicleTagId    = tag.Id,
            PlateNumber     = tag.Vehicle.PlateNumber,
            Type            = ParkingRecordType.Resident,
            EntryEntranceId = entranceId,
            LoggedById      = loggedById,
            EnteredAt       = DateTime.UtcNow
        };

        db.ParkingRecords.Add(record);
        await db.SaveChangesAsync();
        return record;
    }

    public async Task<ParkingRecord> LogVisitorEntryAsync(string plate, string loggedById, Guid? visitId, Guid? entranceId, string? notes)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var record = new ParkingRecord
        {
            TenantId        = _tenantCtx.TenantId,
            PlateNumber     = plate.Trim().ToUpperInvariant(),
            Type            = ParkingRecordType.Visitor,
            VisitId         = visitId,
            EntryEntranceId = entranceId,
            LoggedById      = loggedById,
            EnteredAt       = DateTime.UtcNow,
            Notes           = notes?.Trim()
        };

        db.ParkingRecords.Add(record);
        await db.SaveChangesAsync();
        return record;
    }

    public async Task LogExitAsync(Guid recordId, string loggedById, Guid? exitEntranceId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var record = await db.ParkingRecords.FindAsync(recordId)
            ?? throw new InvalidOperationException("Parking record not found.");

        if (record.ExitedAt.HasValue)
            throw new InvalidOperationException("Exit already recorded for this record.");

        record.ExitedAt       = DateTime.UtcNow;
        record.ExitEntranceId = exitEntranceId;
        await db.SaveChangesAsync();
    }

    public async Task<List<ParkingRecord>> GetCurrentlyInsideAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.ParkingRecords
            .Include(p => p.Vehicle).ThenInclude(v => v!.Owner)
            .Include(p => p.VehicleTag)
            .Include(p => p.Visit).ThenInclude(v => v!.Visitor)
            .Include(p => p.EntryEntrance)
            .Include(p => p.LoggedBy)
            .Where(p => p.ExitedAt == null)
            .OrderByDescending(p => p.EnteredAt)
            .ToListAsync();
    }

    public async Task<List<ParkingRecord>> GetRecordsAsync(DateOnly? from, DateOnly? to)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.ParkingRecords
            .Include(p => p.Vehicle).ThenInclude(v => v!.Owner)
            .Include(p => p.VehicleTag)
            .Include(p => p.Visit).ThenInclude(v => v!.Visitor)
            .Include(p => p.EntryEntrance)
            .Include(p => p.ExitEntrance)
            .Include(p => p.LoggedBy)
            .AsQueryable();

        if (from.HasValue)
        {
            var fromUtc = from.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(p => p.EnteredAt >= fromUtc);
        }
        if (to.HasValue)
        {
            var toUtc = to.Value.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Utc);
            query = query.Where(p => p.EnteredAt <= toUtc);
        }

        return await query.OrderByDescending(p => p.EnteredAt).ToListAsync();
    }

    public async Task<int> GetCurrentlyInsideCountAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.ParkingRecords.CountAsync(p => p.ExitedAt == null);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static async Task<string> GenerateTagNumberAsync(AppDbContext db)
    {
        var allTags = await db.VehicleTags
            .Select(t => t.TagNumber)
            .ToListAsync();

        int max = 0;
        foreach (var num in allTags)
        {
            if (num.StartsWith("TAG-") && int.TryParse(num[4..], out var n) && n > max)
                max = n;
        }

        return $"TAG-{(max + 1):D4}";
    }
}
