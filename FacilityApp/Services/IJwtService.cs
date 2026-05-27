using FacilityApp.Data.Models;

namespace FacilityApp.Services;

public interface IJwtService
{
    string GenerateAccessToken(ApplicationUser user, Guid tenantId, string tenantSlug,
        string tenantName, IList<string> roles);
    Task<string> GenerateRefreshTokenAsync(string userId);
    Task<string?> ValidateRefreshTokenAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(string refreshToken);
    Task RevokeAllUserTokensAsync(string userId);
}
