using FacilityApp.Data.Models;

namespace FacilityApp.Services;

public interface IConsumableService
{
    // Types
    Task<List<ConsumableType>> GetAllTypesAsync();
    Task<ConsumableType> CreateTypeAsync(string name, string unit, int? lowStockThreshold);
    Task RestockAsync(Guid typeId, int quantity, string restockedById, string? notes);
    Task ToggleTypeActiveAsync(Guid typeId);
    Task<List<ConsumableRestockLog>> GetRestockLogsAsync(Guid? typeId = null);

    // Issuances
    Task<List<ConsumableIssuance>> GetIssuancesAsync(Guid? typeId = null, Guid? unitId = null, DateTime? from = null, DateTime? to = null);
    Task<List<ConsumableIssuance>> GetIssuancesForUnitAsync(Guid unitId);
    Task<ConsumableIssuance> IssueAsync(Guid consumableTypeId, Guid unitId, int quantity, DateTime issuedAt, string issuedById, string? notes);
}
