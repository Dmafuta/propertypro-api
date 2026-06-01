using FacilityApp.Data.Models;

namespace FacilityApp.Services;

public interface IPermissionService
{
    Task<HashSet<Permission>> GetPermissionsAsync(string userId);
    void InvalidateUser(string userId);
    void InvalidateAll();
}
