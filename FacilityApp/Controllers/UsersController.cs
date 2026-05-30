using System.Security.Claims;
using FacilityApp.Data.Models;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly TenantContext                _tenantCtx;
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    public UsersController(UserManager<ApplicationUser> users, TenantContext tenantCtx)
    {
        _users     = users;
        _tenantCtx = tenantCtx;
    }

    // GET /api/users
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _users.Users
            .Where(u => u.TenantId == _tenantCtx.TenantId && u.UserType == UserType.Staff)
            .OrderBy(u => u.FullName)
            .ToListAsync();

        var result = new List<object>();
        foreach (var u in users)
        {
            var roles = await _users.GetRolesAsync(u);
            result.Add(new
            {
                u.Id, u.FullName, u.Email, u.PhoneNumber,
                u.UserType, u.CreatedAt,
                Roles = roles
            });
        }
        return Ok(result);
    }

    // GET /api/users/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null || user.TenantId != _tenantCtx.TenantId) return NotFound();

        var roles = await _users.GetRolesAsync(user);
        return Ok(new
        {
            user.Id, user.FullName, user.Email, user.PhoneNumber,
            user.UserType, user.CreatedAt,
            Roles = roles
        });
    }

    // POST /api/users  (create staff member)
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStaffRequest req)
    {
        if (!IsValidStaffRole(req.Role))
            return BadRequest(new { error = "Invalid role." });

        var nameParts = req.FullName.Trim().Split(' ', 2);
        var user = new ApplicationUser
        {
            UserName  = req.Email.Trim().ToLower(),
            Email     = req.Email.Trim().ToLower(),
            FirstName = nameParts[0],
            LastName  = nameParts.Length > 1 ? nameParts[1] : string.Empty,
            TenantId  = _tenantCtx.TenantId,
            UserType  = UserType.Staff
        };

        var result = await _users.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(new { error = result.Errors.FirstOrDefault()?.Description });

        await _users.AddToRoleAsync(user, req.Role);
        return Ok(new { user.Id, user.FullName, user.Email, Role = req.Role });
    }

    // PATCH /api/users/{id}/role
    [HttpPatch("{id}/role")]
    public async Task<IActionResult> UpdateRole(string id, [FromBody] UpdateUserRoleRequest req)
    {
        if (!IsValidStaffRole(req.Role))
            return BadRequest(new { error = "Invalid role." });

        var user = await _users.FindByIdAsync(id);
        if (user is null || user.TenantId != _tenantCtx.TenantId) return NotFound();
        if (user.Id == CurrentUserId) return BadRequest(new { error = "Cannot change your own role." });

        var currentRoles = await _users.GetRolesAsync(user);
        await _users.RemoveFromRolesAsync(user, currentRoles.Where(r => r != "Occupant"));
        await _users.AddToRoleAsync(user, req.Role);
        return NoContent();
    }

    // PATCH /api/users/{id}/phone
    [HttpPatch("{id}/phone")]
    public async Task<IActionResult> UpdatePhone(string id, [FromBody] UpdatePhoneRequest req)
    {
        var user = await _users.FindByIdAsync(id);
        if (user is null || user.TenantId != _tenantCtx.TenantId) return NotFound();

        user.PhoneNumber = req.PhoneNumber?.Trim();
        await _users.UpdateAsync(user);
        return NoContent();
    }

    // DELETE /api/users/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        if (id == CurrentUserId) return BadRequest(new { error = "Cannot delete your own account." });

        var user = await _users.FindByIdAsync(id);
        if (user is null || user.TenantId != _tenantCtx.TenantId) return NotFound();

        await _users.DeleteAsync(user);
        return NoContent();
    }

    private static bool IsValidStaffRole(string role) =>
        role is "Admin" or "Manager" or "Receptionist" or "Security";
}
