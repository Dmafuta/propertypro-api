using FacilityApp.Data;
using FacilityApp.Data.Models;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/superadmin/roles")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin")]
public class RolesController : ControllerBase
{
    private readonly AppDbContext                 _db;
    private readonly RoleManager<IdentityRole>    _roleManager;
    private readonly IPermissionService           _permissions;

    public RolesController(
        AppDbContext db,
        RoleManager<IdentityRole> roleManager,
        IPermissionService permissions)
    {
        _db          = db;
        _roleManager = roleManager;
        _permissions = permissions;
    }

    // GET /api/superadmin/roles
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var roles = await _db.AppRoles
            .IgnoreQueryFilters()
            .Include(r => r.Permissions)
            .OrderBy(r => r.IsSystem ? 0 : 1)
            .ThenBy(r => r.Name)
            .Select(r => new AppRoleDto(
                r.Id, r.Name, r.Description, r.IsSystem, r.IsActive, r.CreatedAt,
                r.Permissions.Select(p => (int)p.Permission).ToList()))
            .ToListAsync();

        return Ok(roles);
    }

    // GET /api/superadmin/roles/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var role = await _db.AppRoles
            .IgnoreQueryFilters()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role is null) return NotFound();

        return Ok(new AppRoleDto(
            role.Id, role.Name, role.Description, role.IsSystem, role.IsActive, role.CreatedAt,
            role.Permissions.Select(p => (int)p.Permission).ToList()));
    }

    // POST /api/superadmin/roles
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { error = "Name is required." });

        var name = req.Name.Trim();

        if (await _db.AppRoles.IgnoreQueryFilters().AnyAsync(r => r.Name == name))
            return Conflict(new { error = "A role with this name already exists." });

        // Create in Identity if it doesn't exist yet
        if (!await _roleManager.RoleExistsAsync(name))
            await _roleManager.CreateAsync(new IdentityRole(name));

        var role = new AppRole
        {
            Name        = name,
            Description = req.Description?.Trim(),
            IsSystem    = false,
            IsActive    = true,
        };
        _db.AppRoles.Add(role);

        // Seed initial permissions
        if (req.Permissions is { Count: > 0 })
        {
            foreach (var p in req.Permissions.Distinct())
            {
                if (Enum.IsDefined(typeof(Permission), p))
                    role.Permissions.Add(new RolePermission { Permission = (Permission)p });
            }
        }

        await _db.SaveChangesAsync();
        _permissions.InvalidateAll();

        return CreatedAtAction(nameof(GetById), new { id = role.Id },
            new AppRoleDto(role.Id, role.Name, role.Description, role.IsSystem, role.IsActive,
                role.CreatedAt, role.Permissions.Select(p => (int)p.Permission).ToList()));
    }

    // PUT /api/superadmin/roles/{id}/permissions  — replace all permissions
    [HttpPut("{id:guid}/permissions")]
    public async Task<IActionResult> UpdatePermissions(Guid id, [FromBody] UpdateRolePermissionsRequest req)
    {
        var role = await _db.AppRoles
            .IgnoreQueryFilters()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (role is null) return NotFound();

        // Validate all values
        var validPermissions = req.Permissions
            .Where(p => Enum.IsDefined(typeof(Permission), p))
            .Select(p => (Permission)p)
            .Distinct()
            .ToHashSet();

        // Remove permissions no longer present
        var toRemove = role.Permissions.Where(rp => !validPermissions.Contains(rp.Permission)).ToList();
        _db.RolePermissions.RemoveRange(toRemove);

        // Add new permissions
        var existing = role.Permissions.Select(rp => rp.Permission).ToHashSet();
        foreach (var p in validPermissions.Where(p => !existing.Contains(p)))
            role.Permissions.Add(new RolePermission { AppRoleId = id, Permission = p });

        await _db.SaveChangesAsync();
        _permissions.InvalidateAll();

        return NoContent();
    }

    // PATCH /api/superadmin/roles/{id}  — update name / description
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest req)
    {
        var role = await _db.AppRoles.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == id);
        if (role is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(req.Description))
            role.Description = req.Description.Trim();

        // Non-system roles can be renamed
        if (!role.IsSystem && !string.IsNullOrWhiteSpace(req.Name))
        {
            var newName = req.Name.Trim();
            if (newName != role.Name)
            {
                if (await _db.AppRoles.IgnoreQueryFilters().AnyAsync(r => r.Name == newName && r.Id != id))
                    return Conflict(new { error = "A role with this name already exists." });

                // Rename in Identity
                var identityRole = await _roleManager.FindByNameAsync(role.Name);
                if (identityRole is not null)
                {
                    identityRole.Name = newName;
                    await _roleManager.UpdateAsync(identityRole);
                }
                role.Name = newName;
            }
        }

        await _db.SaveChangesAsync();
        _permissions.InvalidateAll();
        return NoContent();
    }

    // PATCH /api/superadmin/roles/{id}/toggle
    [HttpPatch("{id:guid}/toggle")]
    public async Task<IActionResult> Toggle(Guid id)
    {
        var role = await _db.AppRoles.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == id);
        if (role is null) return NotFound();

        if (role.IsSystem)
            return BadRequest(new { error = "System roles cannot be deactivated." });

        role.IsActive = !role.IsActive;
        await _db.SaveChangesAsync();
        _permissions.InvalidateAll();
        return NoContent();
    }

    // DELETE /api/superadmin/roles/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var role = await _db.AppRoles.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == id);
        if (role is null) return NotFound();

        if (role.IsSystem)
            return BadRequest(new { error = "System roles cannot be deleted." });

        // Remove from Identity
        var identityRole = await _roleManager.FindByNameAsync(role.Name);
        if (identityRole is not null)
            await _roleManager.DeleteAsync(identityRole);

        _db.AppRoles.Remove(role);
        await _db.SaveChangesAsync();
        _permissions.InvalidateAll();

        return NoContent();
    }

    // GET /api/superadmin/roles/permissions/all  — list all available permission definitions
    [HttpGet("permissions/all")]
    public IActionResult GetAllPermissions()
    {
        var perms = Enum.GetValues<Permission>()
            .Select(p => new PermissionDefinitionDto((int)p, p.ToString()))
            .OrderBy(p => p.Value);
        return Ok(perms);
    }
}
