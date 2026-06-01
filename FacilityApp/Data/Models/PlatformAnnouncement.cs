namespace FacilityApp.Data.Models;

public enum PlatformAudienceFilter { All, Starter, Professional }

public class PlatformAnnouncement
{
    public Guid   Id            { get; set; } = Guid.NewGuid();
    public string Title         { get; set; } = string.Empty;
    public string Body          { get; set; } = string.Empty;
    public AnnouncementCategory  Category { get; set; } = AnnouncementCategory.General;
    public PlatformAudienceFilter Audience { get; set; } = PlatformAudienceFilter.All;
    public bool     IsActive     { get; set; } = true;
    public string   CreatedById  { get; set; } = string.Empty;
    public string   CreatedByName { get; set; } = string.Empty;   // denormalised — avoids cross-tenant filter
    public DateTime PublishedAt  { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt   { get; set; }
}
