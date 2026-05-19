using FacilityApp.Data.Models;

namespace FacilityApp.Services;

public interface IAnnouncementService
{
    Task<List<Announcement>> GetActiveAsync();
    Task<List<Announcement>> GetAllAsync();
    Task<Announcement> CreateAsync(string title, string body, AnnouncementCategory category, DateTime? expiresAt, string createdById);
    Task ToggleActiveAsync(Guid id);
    Task DeleteAsync(Guid id);
}
