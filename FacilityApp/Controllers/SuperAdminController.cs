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
[Route("api/superadmin/tenants")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "SuperAdmin")]
public class SuperAdminController : ControllerBase
{
    private readonly AppDbContext                 _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole>    _roles;
    private readonly IEmailService                _email;
    private readonly ILogger<SuperAdminController> _logger;

    public SuperAdminController(
        AppDbContext db,
        UserManager<ApplicationUser> users,
        RoleManager<IdentityRole> roles,
        IEmailService email,
        ILogger<SuperAdminController> logger)
    {
        _db     = db;
        _users  = users;
        _roles  = roles;
        _email  = email;
        _logger = logger;
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

    // GET /api/superadmin/tenants/{id}/health
    [HttpGet("{id:guid}/health")]
    public async Task<IActionResult> GetHealth(Guid id)
    {
        var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id);
        if (tenant is null) return NotFound();

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var totalStaff = await _db.Users
            .IgnoreQueryFilters()
            .CountAsync(u => u.TenantId == id && u.UserType == UserType.Staff);

        var totalResidents = await _db.Users
            .IgnoreQueryFilters()
            .CountAsync(u => u.TenantId == id && u.UserType != UserType.Staff);

        var visitorVolume30d = await _db.Visits
            .IgnoreQueryFilters()
            .CountAsync(v => v.TenantId == id && v.CheckedInAt >= thirtyDaysAgo);

        var maintenanceBacklog = await _db.MaintenanceRequests
            .IgnoreQueryFilters()
            .CountAsync(m => m.TenantId == id &&
                (m.Status == MaintenanceStatus.Open || m.Status == MaintenanceStatus.InProgress));

        var totalUnits = await _db.Units
            .IgnoreQueryFilters()
            .CountAsync(u => u.TenantId == id);

        var occupiedUnits = await _db.Units
            .IgnoreQueryFilters()
            .CountAsync(u => u.TenantId == id && u.IsOccupied);

        var openIncidents = await _db.IncidentReports
            .IgnoreQueryFilters()
            .CountAsync(r => r.TenantId == id &&
                (r.Status == IncidentStatus.Open || r.Status == IncidentStatus.UnderReview));

        return Ok(new TenantHealthDto(
            totalStaff, totalResidents,
            visitorVolume30d, maintenanceBacklog,
            totalUnits, occupiedUnits, openIncidents));
    }

    // POST /api/superadmin/tenants/{id}/seed-admin
    [HttpPost("{id:guid}/seed-admin")]
    public async Task<IActionResult> SeedAdmin(Guid id, [FromBody] SeedAdminRequest req)
    {
        var tenant = await _db.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == id);
        if (tenant is null) return NotFound(new { error = "Facility not found." });

        var email = req.Email.Trim().ToLower();
        if (await _users.FindByEmailAsync(email) is not null)
            return Conflict(new { error = "A user with this email already exists." });

        // Ensure Admin role exists
        if (!await _roles.RoleExistsAsync("Admin"))
            await _roles.CreateAsync(new IdentityRole("Admin"));

        var user = new ApplicationUser
        {
            FirstName    = req.FirstName.Trim(),
            LastName     = req.LastName.Trim(),
            Email        = email,
            UserName     = email,
            TenantId     = tenant.Id,
            UserType     = UserType.Staff,
            EmailConfirmed = true,
        };

        // Create with a random placeholder password — user must set their own via invite link
        var tempPassword = $"Tmp!{Guid.NewGuid():N}";
        var result = await _users.CreateAsync(user, tempPassword);
        if (!result.Succeeded)
            return BadRequest(new { error = result.Errors.FirstOrDefault()?.Description ?? "Failed to create user." });

        await _users.AddToRoleAsync(user, "Admin");

        // Generate set-password link (reuses the password reset flow)
        var token   = await _users.GeneratePasswordResetTokenAsync(user);
        var encoded = Uri.EscapeDataString(token);
        var setPasswordLink = $"{Request.Scheme}://{Request.Host}/{tenant.Slug}/reset-password?email={Uri.EscapeDataString(email)}&token={encoded}";

        _ = _email.SendAdminInviteAsync(email, user.FullName, tenant.Name, setPasswordLink)
            .ContinueWith(t => _logger.LogError(t.Exception, "Failed to send admin invite email to {Email}", email),
                TaskContinuationOptions.OnlyOnFaulted);

        return Ok(new { userId = user.Id, email = user.Email, fullName = user.FullName });
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
