using FacilityApp.Data.Models;

namespace FacilityApp.Services;

public interface ITenantService
{
    Task<Tenant?> ResolveBySlugAsync(string? slug);
    Task<Tenant?> ResolveByDomainAsync(string? host);
    Task<Tenant?> ResolveByIdAsync(Guid id);
}
