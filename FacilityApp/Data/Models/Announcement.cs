namespace FacilityApp.Data.Models;

public class Announcement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public AnnouncementCategory Category { get; set; } = AnnouncementCategory.General;
    public bool IsActive { get; set; } = true;

    public string CreatedById { get; set; } = string.Empty;
    public ApplicationUser CreatedBy { get; set; } = null!;

    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
}

public enum AnnouncementCategory { General, Maintenance, Event, Urgent }
