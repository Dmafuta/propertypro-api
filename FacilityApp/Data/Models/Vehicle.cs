namespace FacilityApp.Data.Models;

public class Vehicle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    // OwnerId is nullable — VIPs / contractors may not have a system account
    public string? OwnerId { get; set; }
    public ApplicationUser? Owner { get; set; }

    // Display name used when OwnerId is null (VIP / contractor)
    public string? OwnerName { get; set; }
    public OwnerCategory OwnerCategory { get; set; } = OwnerCategory.Resident;

    public string PlateNumber { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Colour { get; set; } = string.Empty;
    public VehicleType Type { get; set; } = VehicleType.Car;
    public string? Notes { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public VehicleTag? Tag { get; set; }
}

public enum VehicleType    { Car, Motorcycle, Truck, Van, Other }
public enum OwnerCategory  { Resident, Staff, VIP, Contractor }
