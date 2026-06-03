using FacilityApp.Data;
using FacilityApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Services;

public class ConsumableService(IDbContextFactory<AppDbContext> factory, TenantContext tenantCtx) : IConsumableService
{
    public async Task<List<ConsumableType>> GetAllTypesAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.ConsumableTypes
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<ConsumableType> CreateTypeAsync(string name, string unit, int? lowStockThreshold)
    {
        await using var db = await factory.CreateDbContextAsync();
        var type = new ConsumableType
        {
            TenantId          = tenantCtx.TenantId,
            Name              = name.Trim(),
            Unit              = unit.Trim(),
            LowStockThreshold = lowStockThreshold,
        };
        db.ConsumableTypes.Add(type);
        await db.SaveChangesAsync();
        return type;
    }

    public async Task RestockAsync(Guid typeId, int quantity)
    {
        if (quantity <= 0) throw new InvalidOperationException("Quantity must be positive.");
        await using var db = await factory.CreateDbContextAsync();
        var type = await db.ConsumableTypes.FindAsync(typeId)
            ?? throw new InvalidOperationException("Consumable type not found.");
        type.CurrentStock += quantity;
        await db.SaveChangesAsync();
    }

    public async Task ToggleTypeActiveAsync(Guid typeId)
    {
        await using var db = await factory.CreateDbContextAsync();
        var type = await db.ConsumableTypes.FindAsync(typeId)
            ?? throw new InvalidOperationException("Consumable type not found.");
        type.IsActive = !type.IsActive;
        await db.SaveChangesAsync();
    }

    public async Task<List<ConsumableIssuance>> GetIssuancesAsync(Guid? typeId = null, Guid? unitId = null, DateTime? from = null, DateTime? to = null)
    {
        await using var db = await factory.CreateDbContextAsync();
        var query = db.ConsumableIssuances
            .Include(i => i.ConsumableType)
            .Include(i => i.Unit)
            .Include(i => i.IssuedBy)
            .AsQueryable();

        if (typeId.HasValue) query = query.Where(i => i.ConsumableTypeId == typeId.Value);
        if (unitId.HasValue) query = query.Where(i => i.UnitId == unitId.Value);
        if (from.HasValue)   query = query.Where(i => i.IssuedAt >= from.Value);
        if (to.HasValue)     query = query.Where(i => i.IssuedAt <= to.Value);

        return await query.OrderByDescending(i => i.IssuedAt).ToListAsync();
    }

    public async Task<List<ConsumableIssuance>> GetIssuancesForUnitAsync(Guid unitId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.ConsumableIssuances
            .Include(i => i.ConsumableType)
            .Include(i => i.IssuedBy)
            .Where(i => i.UnitId == unitId)
            .OrderByDescending(i => i.IssuedAt)
            .ToListAsync();
    }

    public async Task<ConsumableIssuance> IssueAsync(Guid consumableTypeId, Guid unitId, int quantity, DateTime issuedAt, string issuedById, string? notes)
    {
        if (quantity <= 0) throw new InvalidOperationException("Quantity must be positive.");
        await using var db = await factory.CreateDbContextAsync();

        var type = await db.ConsumableTypes.FindAsync(consumableTypeId)
            ?? throw new InvalidOperationException("Consumable type not found.");
        if (type.CurrentStock < quantity)
            throw new InvalidOperationException($"Insufficient stock. Available: {type.CurrentStock} {type.Unit}.");

        type.CurrentStock -= quantity;

        var issuance = new ConsumableIssuance
        {
            TenantId          = tenantCtx.TenantId,
            ConsumableTypeId  = consumableTypeId,
            UnitId            = unitId,
            Quantity          = quantity,
            IssuedAt          = issuedAt,
            IssuedById        = issuedById,
            Notes             = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
        };
        db.ConsumableIssuances.Add(issuance);
        await db.SaveChangesAsync();
        return issuance;
    }
}
