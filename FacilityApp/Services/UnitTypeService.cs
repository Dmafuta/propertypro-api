using FacilityApp.Data;
using FacilityApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Services;

public class UnitTypeService(AppDbContext context, TenantContext tenantCtx) : IUnitTypeService
{
    public async Task<List<UnitType>> GetAllAsync() =>
        await context.UnitTypes
            .OrderBy(t => t.Name)
            .ToListAsync();

    public async Task<UnitType?> GetByIdAsync(Guid id) =>
        await context.UnitTypes.FirstOrDefaultAsync(t => t.Id == id);

    public async Task<UnitType> CreateAsync(string name, string? description,
        decimal? defaultMonthlyLevy, int? defaultBedrooms, int? defaultBathrooms)
    {
        var unitType = new UnitType
        {
            TenantId            = tenantCtx.TenantId,
            Name                = name.Trim(),
            Description         = description?.Trim(),
            DefaultMonthlyLevy  = defaultMonthlyLevy,
            DefaultBedrooms     = defaultBedrooms,
            DefaultBathrooms    = defaultBathrooms,
        };
        context.UnitTypes.Add(unitType);
        await context.SaveChangesAsync();
        return unitType;
    }

    public async Task UpdateAsync(Guid id, string name, string? description,
        decimal? defaultMonthlyLevy, int? defaultBedrooms, int? defaultBathrooms)
    {
        var unitType = await context.UnitTypes.FindAsync(id)
            ?? throw new InvalidOperationException("Unit type not found.");
        unitType.Name               = name.Trim();
        unitType.Description        = description?.Trim();
        unitType.DefaultMonthlyLevy = defaultMonthlyLevy;
        unitType.DefaultBedrooms    = defaultBedrooms;
        unitType.DefaultBathrooms   = defaultBathrooms;
        await context.SaveChangesAsync();
    }

    public async Task ToggleActiveAsync(Guid id)
    {
        var unitType = await context.UnitTypes.FindAsync(id)
            ?? throw new InvalidOperationException("Unit type not found.");
        unitType.IsActive = !unitType.IsActive;
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var unitType = await context.UnitTypes
            .Include(t => t.Units)
            .FirstOrDefaultAsync(t => t.Id == id)
            ?? throw new InvalidOperationException("Unit type not found.");

        if (unitType.Units.Any())
            throw new InvalidOperationException("Cannot delete a unit type that has units assigned. Reassign the units first.");

        context.UnitTypes.Remove(unitType);
        await context.SaveChangesAsync();
    }
}
