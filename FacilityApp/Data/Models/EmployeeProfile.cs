namespace FacilityApp.Data.Models;

public class EmployeeProfile
{
    public Guid   Id       { get; set; } = Guid.NewGuid();
    public Guid   TenantId { get; set; }
    public Tenant Tenant   { get; set; } = null!;

    // One-to-one with ApplicationUser
    public string          UserId { get; set; } = string.Empty;
    public ApplicationUser User   { get; set; } = null!;

    // Personal
    public string?   MiddleName     { get; set; }
    public string?   NationalId     { get; set; }
    public string?   PassportNumber { get; set; }
    public DateTime? DateOfBirth    { get; set; }
    public string?   Gender         { get; set; }
    public string?   Address        { get; set; }

    // Employment
    public DateTime? JoiningDate            { get; set; }
    public string?   ContractType           { get; set; }
    public string?   Department             { get; set; }
    public string?   EmergencyContactName   { get; set; }
    public string?   EmergencyContactPhone  { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
