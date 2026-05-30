using FacilityApp.Data.Models;

namespace FacilityApp.Services;

public interface ISettingsService
{
    Task<Tenant?> GetAsync();
    Task UpdateAsync(string name, string? contactEmail, string? contactPhone, string? address, string? website, string? customDomain);
    Task UpdateBrandingAsync(string? logoUrl, string? primaryColour);
    Task UpdateSmsAsync(bool enabled, string? apiKey, string? username, string? senderId);
}
