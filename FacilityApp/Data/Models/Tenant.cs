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
    public bool        SmsEnabled  { get; set; } = true;
    public SmsProvider SmsProvider { get; set; } = SmsProvider.AfricasTalking;

    // Shared credential fields — semantics differ per provider:
    //   AfricasTalking → ApiKey=AT ApiKey,   Username=AT Username, SenderId=Sender ID
    //   Twilio         → ApiKey=Auth Token,  Username=Account SID, SenderId=From number
    //   Vonage         → ApiKey=API Key,     Username=API Secret,  SenderId=From name
    //   CustomHttp     → ApiKey=Bearer token (optional), SenderId=From (optional)
    public string? SmsApiKey  { get; set; }
    public string? SmsUsername { get; set; }
    public string? SmsSenderId { get; set; }

    /// <summary>Endpoint URL for the CustomHttp provider.</summary>
    public string? SmsApiUrl { get; set; }

    // M-Pesa (Safaricom Daraja) — per-tenant credentials
    public bool    MpesaEnabled        { get; set; } = false;
    public bool    MpesaSandbox        { get; set; } = true;
    public string? MpesaShortCode      { get; set; }
    public string? MpesaConsumerKey    { get; set; }
    public string? MpesaConsumerSecret { get; set; }
    public string? MpesaPasskey        { get; set; }
}

public enum TenantPlan { Starter = 0, Professional = 1 }
public enum SmsProvider { AfricasTalking = 0, Twilio = 1, Vonage = 2, CustomHttp = 3 }
