using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace FacilityApp.Data.Models;

public class ApplicationUser : IdentityUser
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string? SecondaryEmail { get; set; }
    public string? AvatarUrl { get; set; }
    public UserType UserType { get; set; } = UserType.Staff;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CurrentEntranceId { get; set; }
    public ICollection<UserUnit> UserUnits { get; set; } = [];

    /// <summary>Computed display name — not stored in DB.</summary>
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();
}
