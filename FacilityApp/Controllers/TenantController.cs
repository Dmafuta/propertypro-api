using FacilityApp.Data.Models;
using FacilityApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/tenant")]
public class TenantController : ControllerBase
{
    private readonly ITenantService _tenantSvc;

    public TenantController(ITenantService tenantSvc)
    {
        _tenantSvc = tenantSvc;
    }

    // GET /api/tenant
    // Public — called by the Next.js [slug] layout to load branding before login
    [HttpGet]
    public async Task<IActionResult> GetTenant()
    {
        var slug = Request.Headers["X-Tenant-Slug"].FirstOrDefault()?.Trim().ToLower();
        if (string.IsNullOrEmpty(slug))
            return BadRequest(new { error = "X-Tenant-Slug header is required." });

        var tenant = await _tenantSvc.ResolveBySlugAsync(slug);
        if (tenant is null)
            return NotFound(new { error = "Facility not found." });

        return Ok(new TenantPublicDto(
            tenant.Id.ToString(),
            tenant.Name,
            tenant.Slug,
            tenant.Plan == TenantPlan.Professional ? "Professional" : "Starter",
            tenant.LogoUrl,
            tenant.PrimaryColour
        ));
    }
}
