namespace FacilityApp.Data.Models;

public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DocumentCategory Category { get; set; } = DocumentCategory.General;

    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public long FileSize { get; set; }

    public string UploadedById { get; set; } = string.Empty;
    public ApplicationUser UploadedBy { get; set; } = null!;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

public enum DocumentCategory { General, Rules, Levy, Bylaws, Notice, Other }
