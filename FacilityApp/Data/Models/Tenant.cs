namespace FacilityApp.Data.Models;

public class Tenant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Platform system tenant — hidden from customer-facing tenant lists
    public bool IsSystem { get; set; } = false;

    // Subscription plan
    public TenantPlan Plan { get; set; } = TenantPlan.Starter;

    // Settings
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string? Website { get; set; }

    // Branding
    public string? LogoUrl { get; set; }
    public string? PrimaryColour { get; set; }  // hex e.g. #1b6ec2

    // Custom domain (e.g. greatwallgardens.estate) — enables slug-free URLs (Professional plan only)
    public string? CustomDomain { get; set; }

    // SMS notifications
    public bool SmsEnabled { get; set; } = true;
    /// <summary>Tenant-specific Africa's Talking API key (Professional plan only). Null = use platform key.</summary>
    public string? SmsApiKey { get; set; }
    public string? SmsUsername { get; set; }
    public string? SmsSenderId { get; set; }
}

public enum TenantPlan { Starter = 0, Professional = 1 }
