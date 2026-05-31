using FacilityApp.Data.Models;

namespace FacilityApp.Services;

public interface IVisitorService
{
    Task<(List<Visit> Items, int Total)> GetVisitsAsync(string tab, string? search, int page = 1, int pageSize = 25);
    Task<List<Visit>> GetScheduledVisitsAsync(string? search);
    Task<List<Visit>> GetVisitsForHostAsync(string hostUserId);
    Task<Visit> WalkInAsync(string fullName, string email, string phone, string? company, string purpose, string? hostUserId, string? notes = null, string? photoUrl = null, Guid? entranceId = null);
    Task<Visit> PreRegisterAsync(string fullName, string email, string phone, string? company, string purpose, string? hostUserId, DateTime scheduledAt, string? notes = null);
    Task CheckInAsync(Guid visitId, Guid? entranceId = null);
    Task CheckOutAsync(Guid visitId, Guid? entranceId = null);
    Task CancelAsync(Guid visitId);
    Task MarkNoShowAsync(Guid visitId);
    Task<List<ApplicationUser>> GetHostsAsync();
    Task<Visit?> GetVisitByIdAsync(Guid id);
}
