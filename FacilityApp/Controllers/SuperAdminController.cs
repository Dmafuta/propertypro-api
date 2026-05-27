using FacilityApp.Data;
using FacilityApp.Data.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/superadmin/tenants")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin")]
public class SuperAdminController : ControllerBase
{
    private readonly AppDbContext                 _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole>    _roles;

    public SuperAdminController(
        AppDbContext db,
        UserManager<ApplicationUser> users,
        RoleManager<IdentityRole> roles)
    {
        _db    = db;
        _users = users;
        _roles = roles;
    }

    // GET /api/superadmin/tenants
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tenants = await _db.Tenants
            .IgnoreQueryFilters()
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TenantDto(
                t.Id, t.Name, t.Slug, t.IsActive, (int)t.Plan,
                t.CustomDomain, t.ContactEmail, t.ContactPhone,
                t.Address, t.Website, t.PrimaryColour, t.LogoUrl, t.CreatedAt))
            .ToListAsync();

        return Ok(tenants);
    }

    // POST /api/superadmin/tenants
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest req)
    {
        var slug = req.Slug.Trim().ToLower();

        if (await _db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.Slug == slug))
            return Conflict(new { error = "An organisation with this code already exists." });

        var tenant = new Tenant
        {
            Name         = req.Name.Trim(),
            Slug         = slug,
            ContactEmail = req.ContactEmail.Trim(),
            IsActive     = true,
            Plan         = TenantPlan.Starter
        };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();

        // Seed Identity roles for this tenant (roles are global)
        foreach (var role in new[] { "Admin", "Manager", "Receptionist", "Security", "Occupant" })
        {
            if (!await _roles.RoleExistsAsync(role))
                await _roles.CreateAsync(new IdentityRole(role));
        }

        return CreatedAtAction(nameof(GetAll), new TenantDto(
            tenant.Id, tenant.Name, tenant.Slug, tenant.IsActive, (int)tenant.Plan,
            tenant.CustomDomain, tenant.ContactEmail, tenant.ContactPhone,
            tenant.Address, tenant.Website, tenant.PrimaryColour, tenant.LogoUrl, tenant.CreatedAt));
    }

    // PATCH /api/superadmin/tenants/{id}/toggle
    [HttpPatch("{id:guid}/toggle")]
    public async Task<IActionResult> Toggle(Guid id)
    {
        var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id);
        if (tenant is null) return NotFound();

        tenant.IsActive = !tenant.IsActive;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // PATCH /api/superadmin/tenants/{id}/plan
    [HttpPatch("{id:guid}/plan")]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdatePlanRequest req)
    {
        var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id);
        if (tenant is null) return NotFound();

        if (!Enum.IsDefined(typeof(TenantPlan), req.Plan))
            return BadRequest(new { error = "Invalid plan value." });

        tenant.Plan = (TenantPlan)req.Plan;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
