using FacilityApp.Data;
using FacilityApp.Data.Models;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/hr")]
[Authorize]
public class HrController : ControllerBase
{
    private readonly AppDbContext       _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly TenantContext      _tenantCtx;

    public HrController(
        AppDbContext db,
        UserManager<ApplicationUser> users,
        TenantContext tenantCtx)
    {
        _db        = db;
        _users     = users;
        _tenantCtx = tenantCtx;
    }

    // GET /api/hr/staff
    [HttpGet("staff")]
    [Authorize(Roles = "Admin,Manager,HrManager")]
    public async Task<IActionResult> GetStaff(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var query = _db.Users
            .Where(u => u.UserType == UserType.Staff);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(u =>
                u.FullName.ToLower().Contains(s) ||
                u.Email!.ToLower().Contains(s));
        }

        var total = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                u.CreatedAt,
                u.LockoutEnabled,
                u.LockoutEnd,
            })
            .ToListAsync();

        var profiles = await _db.EmployeeProfiles
            .Where(e => users.Select(u => u.Id).Contains(e.UserId))
            .ToDictionaryAsync(e => e.UserId);

        var result = new List<object>();
        foreach (var u in users)
        {
            var appUser = await _users.FindByIdAsync(u.Id);
            var roles   = appUser is null ? Array.Empty<string>() : (await _users.GetRolesAsync(appUser)).ToArray();
            var isActive = !(u.LockoutEnabled && u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow);
            profiles.TryGetValue(u.Id, out var ep);

            result.Add(new
            {
                id          = u.Id,
                fullName    = u.FullName,
                email       = u.Email,
                phoneNumber = u.PhoneNumber,
                roles,
                department   = ep?.Department,
                contractType = ep?.ContractType,
                joiningDate  = ep?.JoiningDate ?? u.CreatedAt,
                isActive,
                createdAt = u.CreatedAt,
                profile   = ep is null ? null : MapProfile(ep),
            });
        }

        return Ok(new { items = result, total, page, pageSize });
    }

    // GET /api/hr/staff/{userId}
    [HttpGet("staff/{userId}")]
    [Authorize(Roles = "Admin,Manager,HrManager")]
    public async Task<IActionResult> GetStaffMember(string userId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.UserType == UserType.Staff);
        if (user is null) return NotFound();

        var roles    = (await _users.GetRolesAsync(user)).ToArray();
        var isActive = !(user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow);
        var ep       = await _db.EmployeeProfiles.FirstOrDefaultAsync(e => e.UserId == userId);

        return Ok(new
        {
            id          = user.Id,
            fullName    = user.FullName,
            email       = user.Email,
            phoneNumber = user.PhoneNumber,
            roles,
            department   = ep?.Department,
            contractType = ep?.ContractType,
            joiningDate  = ep?.JoiningDate ?? user.CreatedAt,
            isActive,
            createdAt = user.CreatedAt,
            profile   = ep is null ? null : MapProfile(ep),
        });
    }

    // PUT /api/hr/staff/{userId}/profile
    [HttpPut("staff/{userId}/profile")]
    [Authorize(Roles = "Admin,Manager,HrManager")]
    public async Task<IActionResult> UpsertProfile(string userId, [FromBody] UpsertEmployeeProfileRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId && u.UserType == UserType.Staff);
        if (user is null) return NotFound();

        var ep = await _db.EmployeeProfiles.FirstOrDefaultAsync(e => e.UserId == userId);
        if (ep is null)
        {
            ep = new EmployeeProfile
            {
                TenantId = _tenantCtx.TenantId,
                UserId   = userId,
            };
            _db.EmployeeProfiles.Add(ep);
        }

        ep.MiddleName            = req.MiddleName;
        ep.NationalId            = req.NationalId;
        ep.PassportNumber        = req.PassportNumber;
        ep.DateOfBirth           = req.DateOfBirth;
        ep.Gender                = req.Gender;
        ep.Address               = req.Address;
        ep.JoiningDate           = req.JoiningDate;
        ep.ContractType          = req.ContractType;
        ep.Department            = req.Department;
        ep.EmergencyContactName  = req.EmergencyContactName;
        ep.EmergencyContactPhone = req.EmergencyContactPhone;
        ep.UpdatedAt             = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapProfile(ep));
    }

    private static EmployeeProfileDto MapProfile(EmployeeProfile ep) => new(
        ep.MiddleName, ep.NationalId, ep.PassportNumber,
        ep.DateOfBirth, ep.Gender, ep.Address,
        ep.JoiningDate, ep.ContractType, ep.Department,
        ep.EmergencyContactName, ep.EmergencyContactPhone);
}
