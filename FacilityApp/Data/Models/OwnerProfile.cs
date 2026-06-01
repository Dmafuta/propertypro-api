namespace FacilityApp.Data.Models;

/// <summary>
/// Homeowner-specific extended profile.
/// Stores financial, property and managing-agent data.
/// One-to-one with ApplicationUser (only HomeOwner UserType).
/// </summary>
public class OwnerProfile
{
    public Guid   Id       { get; set; } = Guid.NewGuid();
    public Guid   TenantId { get; set; }
    public Tenant Tenant   { get; set; } = null!;

    public string          UserId { get; set; } = string.Empty;
    public ApplicationUser User   { get; set; } = null!;

    // Tax / financial
    public string? KraPin            { get; set; }
    public string? BankName          { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankBranch        { get; set; }
    public string? LevyPaymentMethod { get; set; }

    // Property
    public string? TitleDeedRef    { get; set; }
    public bool    IsAbsenteeOwner { get; set; }

    // Managing agent (populated when IsAbsenteeOwner = true)
    public string? ManagingAgentName    { get; set; }
    public string? ManagingAgentContact { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
