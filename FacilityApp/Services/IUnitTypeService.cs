using FacilityApp.Data.Models;

namespace FacilityApp.Services;

public interface IUnitTypeService
{
    Task<List<UnitType>> GetAllAsync();
    Task<UnitType?> GetByIdAsync(Guid id);
    Task<UnitType> CreateAsync(string name, string? description, decimal? defaultMonthlyLevy, int? defaultBedrooms, int? defaultBathrooms);
    Task UpdateAsync(Guid id, string name, string? description, decimal? defaultMonthlyLevy, int? defaultBedrooms, int? defaultBathrooms);
    Task ToggleActiveAsync(Guid id);
    Task DeleteAsync(Guid id);
}
