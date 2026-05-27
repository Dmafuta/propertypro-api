using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FacilityApp.Data;
using FacilityApp.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FacilityApp.Services;

public class JwtSettings
{
    public string Secret              { get; set; } = "";
    public string Issuer              { get; set; } = "FacilityApp";
    public string Audience            { get; set; } = "FacilityApp";
    public int    AccessTokenMinutes  { get; set; } = 60;
    public int    RefreshTokenDays    { get; set; } = 30;
}

public class JwtService : IJwtService
{
    private readonly JwtSettings  _settings;
    private readonly AppDbContext _db;

    public JwtService(JwtSettings settings, AppDbContext db)
    {
        _settings = settings;
        _db       = db;
    }

    public string GenerateAccessToken(ApplicationUser user, Guid tenantId, string tenantSlug,
        string tenantName, IList<string> roles)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub,   user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(JwtRegisteredClaimNames.Name,  user.FullName),
            new(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
            new("tenant_id",   tenantId.ToString()),
            new("tenant_slug", tenantSlug),
            new("tenant_name", tenantName),
            new("user_type",   user.UserType.ToString()),
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer:             _settings.Issuer,
            audience:           _settings.Audience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<string> GenerateRefreshTokenAsync(string userId)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId    = userId,
            Token     = token,
            ExpiresAt = DateTime.UtcNow.AddDays(_settings.RefreshTokenDays)
        });
        await _db.SaveChangesAsync();

        return token;
    }

    /// <summary>Returns the UserId if the token is valid, null otherwise.</summary>
    public async Task<string?> ValidateRefreshTokenAsync(string refreshToken)
    {
        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == refreshToken
                                   && !r.IsRevoked
                                   && r.ExpiresAt > DateTime.UtcNow);
        return stored?.UserId;
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == refreshToken);
        if (stored is not null)
        {
            stored.IsRevoked = true;
            await _db.SaveChangesAsync();
        }
    }

    public async Task RevokeAllUserTokensAsync(string userId)
    {
        await _db.RefreshTokens
            .Where(r => r.UserId == userId && !r.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsRevoked, true));
    }
}
