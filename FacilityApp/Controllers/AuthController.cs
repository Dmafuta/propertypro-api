using System.Security.Claims;
using FacilityApp.Data;
using FacilityApp.Data.Models;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser>   _users;
    private readonly ITenantService                 _tenantSvc;
    private readonly IJwtService                    _jwt;
    private readonly IEmailService                  _email;
    private readonly JwtSettings                    _jwtSettings;
    private readonly AppDbContext                   _db;

    public AuthController(
        UserManager<ApplicationUser> users,
        ITenantService tenantSvc,
        IJwtService jwt,
        IEmailService email,
        JwtSettings jwtSettings,
        AppDbContext db)
    {
        _users       = users;
        _tenantSvc   = tenantSvc;
        _jwt         = jwt;
        _email       = email;
        _jwtSettings = jwtSettings;
        _db          = db;
    }

    // ── Staff Login ─────────────────────────────────────────────────────────

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        var tenant = await _tenantSvc.ResolveBySlugAsync(req.Slug);
        if (tenant is null)
            return NotFound(new { error = "Facility not found." });

        var user = await _users.FindByEmailAsync(req.Email.Trim().ToLower());
        if (user is null || user.TenantId != tenant.Id || user.UserType != UserType.Staff)
            return Unauthorized(new { error = "Invalid email or password." });

        var ok = await _users.CheckPasswordAsync(user, req.Password);
        if (!ok)
            return Unauthorized(new { error = "Invalid email or password." });

        return Ok(await BuildLoginResponseAsync(user, tenant));
    }

    // ── Resident Login ──────────────────────────────────────────────────────

    [HttpPost("resident/login")]
    public async Task<IActionResult> ResidentLogin([FromBody] LoginRequest req)
    {
        var tenant = await _tenantSvc.ResolveBySlugAsync(req.Slug);
        if (tenant is null)
            return NotFound(new { error = "Facility not found." });

        var user = await _users.FindByEmailAsync(req.Email.Trim().ToLower());
        if (user is null || user.TenantId != tenant.Id ||
            (user.UserType != UserType.HomeOwner && user.UserType != UserType.Resident))
            return Unauthorized(new { error = "Invalid email or password." });

        var ok = await _users.CheckPasswordAsync(user, req.Password);
        if (!ok)
            return Unauthorized(new { error = "Invalid email or password." });

        return Ok(await BuildLoginResponseAsync(user, tenant));
    }

    // ── SuperAdmin Login ────────────────────────────────────────────────────

    [HttpPost("superadmin/login")]
    public async Task<IActionResult> SuperAdminLogin([FromBody] SuperAdminLoginRequest req)
    {
        // Must use IgnoreQueryFilters — the global TenantId filter blocks cross-tenant lookup
        var normalised = req.Email.Trim().ToUpper();
        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalised);
        if (user is null)
            return Unauthorized(new { error = "Invalid credentials." });

        var roles = await _users.GetRolesAsync(user);
        if (!roles.Contains(FacilityApp.Program.RoleSuperAdmin))
            return Unauthorized(new { error = "Invalid credentials." });

        var ok = await _users.CheckPasswordAsync(user, req.Password);
        if (!ok)
            return Unauthorized(new { error = "Invalid credentials." });

        var accessToken  = _jwt.GenerateAccessToken(user, Guid.Empty, "platform", "Platform", roles);
        var refreshToken = await _jwt.GenerateRefreshTokenAsync(user.Id);
        var expiresAt    = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenMinutes);

        return Ok(new LoginResponse(
            accessToken, refreshToken, expiresAt,
            new UserDto(user.Id, user.FullName, user.Email ?? "", roles.ToArray(),
                Guid.Empty.ToString(), "platform", "Platform", user.UserType.ToString())));
    }

    // ── Refresh Token ───────────────────────────────────────────────────────

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
    {
        var userId = await _jwt.ValidateRefreshTokenAsync(req.RefreshToken);
        if (userId is null)
            return Unauthorized(new { error = "Invalid or expired refresh token." });

        var user = await _users.FindByIdAsync(userId);
        if (user is null)
            return Unauthorized(new { error = "User not found." });

        var tenant = await _tenantSvc.ResolveByIdAsync(user.TenantId);
        var roles  = await _users.GetRolesAsync(user);

        // Rotate refresh token
        await _jwt.RevokeRefreshTokenAsync(req.RefreshToken);
        var newRefresh = await _jwt.GenerateRefreshTokenAsync(user.Id);

        var tenantSlug = tenant?.Slug ?? "platform";
        var tenantName = tenant?.Name ?? "Platform";
        var tenantId   = tenant?.Id   ?? Guid.Empty;

        var accessToken = _jwt.GenerateAccessToken(user, tenantId, tenantSlug, tenantName, roles);
        var expiresAt   = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenMinutes);

        return Ok(new { accessToken, refreshToken = newRefresh, expiresAt });
    }

    // ── Logout ──────────────────────────────────────────────────────────────

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest req)
    {
        await _jwt.RevokeRefreshTokenAsync(req.RefreshToken);
        return NoContent();
    }

    // ── Forgot Password ─────────────────────────────────────────────────────

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
    {
        // Always return 204 to prevent user enumeration
        var tenant = await _tenantSvc.ResolveBySlugAsync(req.Slug);
        if (tenant is null)
            return NoContent();

        var user = await _users.FindByEmailAsync(req.Email.Trim().ToLower());
        if (user is null || user.TenantId != tenant.Id)
            return NoContent();

        var token     = await _users.GeneratePasswordResetTokenAsync(user);
        var encoded   = Uri.EscapeDataString(token);
        var resetLink = $"{Request.Scheme}://{Request.Host}/{req.Slug}/reset-password?email={Uri.EscapeDataString(user.Email ?? "")}&token={encoded}";

        _ = _email.SendPasswordResetAsync(user.Email!, user.FullName, resetLink);
        return NoContent();
    }

    // ── Reset Password ──────────────────────────────────────────────────────

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        var tenant = await _tenantSvc.ResolveBySlugAsync(req.Slug);
        if (tenant is null)
            return BadRequest(new { error = "Facility not found." });

        var user = await _users.FindByEmailAsync(req.Email.Trim().ToLower());
        if (user is null || user.TenantId != tenant.Id)
            return BadRequest(new { error = "Invalid request." });

        var result = await _users.ResetPasswordAsync(user, req.Token, req.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { error = result.Errors.FirstOrDefault()?.Description ?? "Reset failed." });

        await _jwt.RevokeAllUserTokensAsync(user.Id);
        return NoContent();
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task<LoginResponse> BuildLoginResponseAsync(ApplicationUser user, Data.Models.Tenant tenant)
    {
        var roles        = await _users.GetRolesAsync(user);
        var accessToken  = _jwt.GenerateAccessToken(user, tenant.Id, tenant.Slug, tenant.Name, roles);
        var refreshToken = await _jwt.GenerateRefreshTokenAsync(user.Id);
        var expiresAt    = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenMinutes);

        return new LoginResponse(
            accessToken, refreshToken, expiresAt,
            new UserDto(user.Id, user.FullName, user.Email ?? "", roles.ToArray(),
                tenant.Id.ToString(), tenant.Slug, tenant.Name, user.UserType.ToString(),
                tenant.PrimaryColour, tenant.LogoUrl));
    }
}

public record SuperAdminLoginRequest(string Email, string Password);
