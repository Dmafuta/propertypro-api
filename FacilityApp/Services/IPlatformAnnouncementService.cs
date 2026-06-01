using FacilityApp.Data.Models;

namespace FacilityApp.Services;

public interface IPlatformAnnouncementService
{
    Task<List<PlatformAnnouncement>> GetAllAsync();
    Task<PlatformAnnouncement> CreateAsync(
        string title, string body,
        AnnouncementCategory category,
        PlatformAudienceFilter audience,
        DateTime? expiresAt,
        string createdById,
        string createdByName);
    Task ToggleActiveAsync(Guid id);
    Task DeleteAsync(Guid id);
}
