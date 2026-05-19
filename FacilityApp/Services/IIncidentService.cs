using FacilityApp.Data.Models;

namespace FacilityApp.Services;

public interface IIncidentService
{
    Task<List<IncidentReport>> GetAllAsync();
    Task<int> GetOpenCountAsync();
    Task<IncidentReport> CreateAsync(string title, string description, string location,
        string? involvedParties, IncidentCategory category, IncidentSeverity severity, string reportedById);
    Task UpdateStatusAsync(Guid id, IncidentStatus status, string? resolutionNotes, string resolvedById);
    Task DeleteAsync(Guid id);
}
