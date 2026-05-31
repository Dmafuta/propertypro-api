using FacilityApp.Data.Models;

namespace FacilityApp.Services;

public record UnitDetails(
    Unit Unit,
    ApplicationUser? Owner,
    List<ApplicationUser> Occupants
);

public interface IUnitService
{
    Task<List<UnitDetails>> GetAllAsync();
    Task<UnitDetails?> GetByIdAsync(Guid unitId);
    Task<Unit> CreateAsync(string unitNumber, string? block, string? floor, string? description,
        Guid? unitTypeId, UnitStatus status, decimal? sizeM2, int? bedrooms, int? bathrooms,
        int parkingBays, decimal? monthlyLevy, string? notes);
    Task UpdateAsync(Guid unitId, string unitNumber, string? block, string? floor, string? description,
        Guid? unitTypeId, UnitStatus status, decimal? sizeM2, int? bedrooms, int? bathrooms,
        int parkingBays, decimal? monthlyLevy, string? notes);
    Task PatchStatusAsync(Guid unitId, UnitStatus status);
    Task DeleteAsync(Guid unitId);

    // Owner management
    Task AssignOwnerAsync(Guid unitId, string userId);
    Task RemoveOwnerAsync(Guid unitId);

    // Occupant management (supports multiple occupants)
    Task AddOccupantAsync(Guid unitId, string userId, DateTime? moveInDate);
    Task RemoveOccupantAsync(Guid unitId, string userId, DateTime? moveOutDate);

    Task<List<ApplicationUser>> GetAssignableUsersAsync(UnitLinkType linkType);
    Task<List<(Unit Unit, UnitLinkType LinkType)>> GetForUserAsync(string userId);
}
