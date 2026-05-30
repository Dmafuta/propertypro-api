using System.Security.Claims;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/account")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AccountController : ControllerBase
{
    private readonly IAccountService _account;
    private readonly TenantContext   _tenantCtx;

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    public AccountController(IAccountService account, TenantContext tenantCtx)
    {
        _account   = account;
        _tenantCtx = tenantCtx;
    }

    // GET /api/account/profile
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await _account.GetProfileAsync(CurrentUserId);
        if (profile is null) return NotFound();
        return Ok(profile);
    }

    // PATCH /api/account/profile  (name fields)
    [HttpPatch("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.LastName))
            return BadRequest(new { error = "First name and last name are required." });

        try
        {
            await _account.UpdateProfileAsync(CurrentUserId, req.FirstName, req.MiddleName, req.LastName);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // PATCH /api/account/username
    [HttpPatch("username")]
    public async Task<IActionResult> UpdateUsername([FromBody] UpdateUsernameRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.UserName))
            return BadRequest(new { error = "Username is required." });

        var error = await _account.UpdateUsernameAsync(CurrentUserId, req.UserName);
        if (error is not null) return BadRequest(new { error });
        return NoContent();
    }

    // PATCH /api/account/email
    [HttpPatch("email")]
    public async Task<IActionResult> UpdateEmail([FromBody] UpdateEmailRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.PrimaryEmail))
            return BadRequest(new { error = "Primary email is required." });

        var error = await _account.UpdateEmailAsync(CurrentUserId, req.PrimaryEmail, req.SecondaryEmail);
        if (error is not null) return BadRequest(new { error });
        return NoContent();
    }

    // POST /api/account/avatar
    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        try
        {
            var slug = _tenantCtx.IsResolved ? _tenantCtx.TenantSlug : "platform";
            var url  = await _account.UploadAvatarAsync(CurrentUserId, slug, file);
            return Ok(new AvatarUploadResponse(url));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
