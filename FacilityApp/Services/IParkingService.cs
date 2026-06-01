using FacilityApp.Data.Models;

namespace FacilityApp.Services;

public interface IParkingService
{
    // Vehicles
    Task<List<Vehicle>> GetVehiclesForResidentAsync(string residentId);
    Task<List<Vehicle>> GetAllVehiclesAsync();
    Task<Vehicle?> GetVehicleByIdAsync(Guid vehicleId);
    Task<Vehicle> RegisterVehicleAsync(string? ownerId, string? ownerName, OwnerCategory ownerCategory, string plate, string make, string model, string colour, VehicleType type, string? notes);
    Task DeleteVehicleAsync(Guid vehicleId);

    // Tags
    Task<VehicleTag> IssueTagAsync(Guid vehicleId, string issuedById, DateTime? expiresAt, string? notes);
    Task UpdateTagStatusAsync(Guid tagId, TagStatus status);
    Task<VehicleTag?> LookupTagAsync(string tagNumber);
    Task SuspendTagsByUserAsync(string userId);

    // Parking records
    Task<ParkingRecord> LogEntryByTagAsync(string tagNumber, string loggedById, Guid? entranceId);
    Task<ParkingRecord> LogVisitorEntryAsync(string plate, string loggedById, Guid? visitId, Guid? entranceId, string? notes);
    Task LogExitAsync(Guid recordId, string loggedById, Guid? exitEntranceId);
    Task<List<ParkingRecord>> GetCurrentlyInsideAsync();
    Task<List<ParkingRecord>> GetRecordsAsync(DateOnly? from, DateOnly? to);
    Task<int> GetCurrentlyInsideCountAsync();
}
