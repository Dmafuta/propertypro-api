namespace FacilityApp.Data.Models;

/// <summary>
/// Extended profile for all resident-portal users (HomeOwner + Resident).
/// Stores identity, address, emergency contacts and next-of-kin data.
/// One-to-one with ApplicationUser.
/// </summary>
public class ResidentProfile
{
    public Guid   Id       { get; set; } = Guid.NewGuid();
    public Guid   TenantId { get; set; }
    public Tenant Tenant   { get; set; } = null!;

    public string          UserId { get; set; } = string.Empty;
    public ApplicationUser User   { get; set; } = null!;

    // Identity
    public string?   NationalId     { get; set; }
    public string?   PassportNumber { get; set; }
    public DateTime? DateOfBirth    { get; set; }
    public string?   Gender         { get; set; }

    // Address
    public string? PhysicalAddress { get; set; }

    // Emergency contact
    public string? EmergencyContactName  { get; set; }
    public string? EmergencyContactPhone { get; set; }

    // Next of kin
    public string? NextOfKinName         { get; set; }
    public string? NextOfKinPhone        { get; set; }
    public string? NextOfKinRelationship { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
