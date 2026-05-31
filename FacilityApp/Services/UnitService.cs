using FacilityApp.Data;
using FacilityApp.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Services;

public class UnitService : IUnitService
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TenantContext _tenantCtx;

    public UnitService(AppDbContext context, UserManager<ApplicationUser> userManager, TenantContext tenantCtx)
    {
        _context     = context;
        _userManager = userManager;
        _tenantCtx   = tenantCtx;
    }

    public async Task<List<UnitDetails>> GetAllAsync()
    {
        var units = await _context.Units
            .Include(u => u.UnitType)
            .Include(u => u.UserUnits.Where(uu => uu.MoveOutDate == null))
                .ThenInclude(uu => uu.User)
            .Include(u => u.Meters.Where(m => m.IsActive))
            .OrderBy(u => u.Block)
            .ThenBy(u => u.UnitNumber)
            .ToListAsync();

        return units.Select(u => new UnitDetails(
            u,
            u.UserUnits.FirstOrDefault(uu => uu.LinkType == UnitLinkType.Owner)?.User,
            u.UserUnits.Where(uu => uu.LinkType == UnitLinkType.Occupant).Select(uu => uu.User).ToList()
        )).ToList();
    }

    public async Task<UnitDetails?> GetByIdAsync(Guid unitId)
    {
        var unit = await _context.Units
            .Include(u => u.UnitType)
            .Include(u => u.UserUnits)
                .ThenInclude(uu => uu.User)
            .Include(u => u.Meters)
            .FirstOrDefaultAsync(u => u.Id == unitId);

        if (unit is null) return null;

        return new UnitDetails(
            unit,
            unit.UserUnits.FirstOrDefault(uu => uu.LinkType == UnitLinkType.Owner)?.User,
            unit.UserUnits.Where(uu => uu.LinkType == UnitLinkType.Occupant && uu.MoveOutDate == null)
                .Select(uu => uu.User).ToList()
        );
    }

    public async Task<Unit> CreateAsync(string unitNumber, string? block, string? floor, string? description,
        Guid? unitTypeId, UnitStatus status, decimal? sizeM2, int? bedrooms, int? bathrooms,
        int parkingBays, decimal? monthlyLevy, string? notes)
    {
        var unit = new Unit
        {
            TenantId    = _tenantCtx.TenantId,
            UnitNumber  = unitNumber.Trim(),
            Block       = block?.Trim(),
            Floor       = floor?.Trim(),
            Description = description?.Trim(),
            UnitTypeId  = unitTypeId,
            Status      = status,
            SizeM2      = sizeM2,
            Bedrooms    = bedrooms,
            Bathrooms   = bathrooms,
            ParkingBays = parkingBays,
            MonthlyLevy = monthlyLevy,
            Notes       = notes?.Trim(),
        };
        _context.Units.Add(unit);
        await _context.SaveChangesAsync();
        return unit;
    }

    public async Task UpdateAsync(Guid unitId, string unitNumber, string? block, string? floor, string? description,
        Guid? unitTypeId, UnitStatus status, decimal? sizeM2, int? bedrooms, int? bathrooms,
        int parkingBays, decimal? monthlyLevy, string? notes)
    {
        var unit = await _context.Units.FindAsync(unitId)
            ?? throw new InvalidOperationException("Unit not found.");
        unit.UnitNumber  = unitNumber.Trim();
        unit.Block       = block?.Trim();
        unit.Floor       = floor?.Trim();
        unit.Description = description?.Trim();
        unit.UnitTypeId  = unitTypeId;
        unit.Status      = status;
        unit.SizeM2      = sizeM2;
        unit.Bedrooms    = bedrooms;
        unit.Bathrooms   = bathrooms;
        unit.ParkingBays = parkingBays;
        unit.MonthlyLevy = monthlyLevy;
        unit.Notes       = notes?.Trim();
        await _context.SaveChangesAsync();
    }

    public async Task PatchStatusAsync(Guid unitId, UnitStatus status)
    {
        var unit = await _context.Units.FindAsync(unitId)
            ?? throw new InvalidOperationException("Unit not found.");
        unit.Status = status;
        await _context.SaveChangesAsync();
    }

    // ── Owner ─────────────────────────────────────────────────────────────────

    public async Task AssignOwnerAsync(Guid unitId, string userId)
    {
        var existing = await _context.UserUnits
            .FirstOrDefaultAsync(uu => uu.UnitId == unitId && uu.LinkType == UnitLinkType.Owner);
        if (existing is not null)
            _context.UserUnits.Remove(existing);

        _context.UserUnits.Add(new UserUnit
        {
            TenantId = _tenantCtx.TenantId,
            UnitId   = unitId,
            UserId   = userId,
            LinkType = UnitLinkType.Owner,
        });
        await _context.SaveChangesAsync();
    }

    public async Task RemoveOwnerAsync(Guid unitId)
    {
        var link = await _context.UserUnits
            .FirstOrDefaultAsync(uu => uu.UnitId == unitId && uu.LinkType == UnitLinkType.Owner);
        if (link is null) return;
        _context.UserUnits.Remove(link);
        await _context.SaveChangesAsync();
    }

    // ── Occupants (multiple supported) ────────────────────────────────────────

    public async Task AddOccupantAsync(Guid unitId, string userId, DateTime? moveInDate)
    {
        // Check if already an active occupant of this unit
        var existing = await _context.UserUnits
            .FirstOrDefaultAsync(uu => uu.UnitId == unitId && uu.UserId == userId
                && uu.LinkType == UnitLinkType.Occupant && uu.MoveOutDate == null);
        if (existing is not null)
            throw new InvalidOperationException("This user is already an active occupant of this unit.");

        _context.UserUnits.Add(new UserUnit
        {
            TenantId    = _tenantCtx.TenantId,
            UnitId      = unitId,
            UserId      = userId,
            LinkType    = UnitLinkType.Occupant,
            MoveInDate  = moveInDate ?? DateTime.UtcNow,
        });

        // Update unit status to Occupied
        var unit = await _context.Units.FindAsync(unitId);
        if (unit is not null && unit.Status == UnitStatus.Available)
            unit.Status = UnitStatus.Occupied;

        await _context.SaveChangesAsync();

        // Grant Occupant role
        var user = await _userManager.FindByIdAsync(userId);
        if (user is not null && !await _userManager.IsInRoleAsync(user, Program.RoleOccupant))
            await _userManager.AddToRoleAsync(user, Program.RoleOccupant);
    }

    public async Task RemoveOccupantAsync(Guid unitId, string userId, DateTime? moveOutDate)
    {
        var link = await _context.UserUnits
            .FirstOrDefaultAsync(uu => uu.UnitId == unitId && uu.UserId == userId
                && uu.LinkType == UnitLinkType.Occupant && uu.MoveOutDate == null)
            ?? throw new InvalidOperationException("Active occupant link not found.");

        link.MoveOutDate = moveOutDate ?? DateTime.UtcNow;

        // If no remaining occupants, mark unit as Vacant
        var remainingOccupants = await _context.UserUnits
            .CountAsync(uu => uu.UnitId == unitId && uu.LinkType == UnitLinkType.Occupant
                && uu.MoveOutDate == null && uu.UserId != userId);

        if (remainingOccupants == 0)
        {
            var unit = await _context.Units.FindAsync(unitId);
            if (unit is not null && unit.Status == UnitStatus.Occupied)
                unit.Status = UnitStatus.Vacant;
        }

        await _context.SaveChangesAsync();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is not null)
            await RevokeOccupantRoleIfUnlinkedAsync(user);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    public async Task DeleteAsync(Guid unitId)
    {
        var unit = await _context.Units
            .Include(u => u.UserUnits)
            .FirstOrDefaultAsync(u => u.Id == unitId)
            ?? throw new InvalidOperationException("Unit not found.");

        if (unit.UserUnits.Any(uu => uu.LinkType == UnitLinkType.Occupant && uu.MoveOutDate == null))
            throw new InvalidOperationException("Cannot delete a unit with active occupants. Remove occupants first.");

        _context.UserUnits.RemoveRange(unit.UserUnits);
        _context.Units.Remove(unit);
        await _context.SaveChangesAsync();
    }

    public async Task<List<ApplicationUser>> GetAssignableUsersAsync(UnitLinkType linkType)
    {
        var users = _context.Users.AsQueryable();
        return linkType == UnitLinkType.Owner
            ? await users.Where(u => u.UserType == UserType.HomeOwner)
                         .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
                         .ToListAsync()
            : await users.Where(u => u.UserType == UserType.Resident || u.UserType == UserType.HomeOwner)
                         .OrderBy(u => u.UserType).ThenBy(u => u.FirstName).ThenBy(u => u.LastName)
                         .ToListAsync();
    }

    public async Task<List<(Unit Unit, UnitLinkType LinkType)>> GetForUserAsync(string userId)
    {
        var links = await _context.UserUnits
            .Include(uu => uu.Unit)
            .Where(uu => uu.UserId == userId)
            .OrderBy(uu => uu.Unit.Block)
            .ThenBy(uu => uu.Unit.UnitNumber)
            .ToListAsync();

        return links.Select(uu => (uu.Unit, uu.LinkType)).ToList();
    }

    private async Task RevokeOccupantRoleIfUnlinkedAsync(ApplicationUser user)
    {
        var stillOccupant = await _context.UserUnits
            .AnyAsync(uu => uu.UserId == user.Id && uu.LinkType == UnitLinkType.Occupant && uu.MoveOutDate == null);

        if (!stillOccupant && await _userManager.IsInRoleAsync(user, Program.RoleOccupant))
            await _userManager.RemoveFromRoleAsync(user, Program.RoleOccupant);
    }
}
