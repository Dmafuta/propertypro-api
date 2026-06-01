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
[Route("api/residents")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme,
           Roles = "Admin,Manager")]
public class ResidentsController : ControllerBase
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly UserManager<ApplicationUser>    _users;
    private readonly TenantContext                   _tenantCtx;

    public ResidentsController(
        IDbContextFactory<AppDbContext> factory,
        UserManager<ApplicationUser>    users,
        TenantContext                   tenantCtx)
    {
        _factory   = factory;
        _users     = users;
        _tenantCtx = tenantCtx;
    }

    // ── GET /api/residents ────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var query = db.Users
            .Where(u => u.UserType == UserType.HomeOwner || u.UserType == UserType.Resident)
            .Include(u => u.UserUnits).ThenInclude(uu => uu.Unit)
            .Include(u => u.ResidentProfile)
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .AsQueryable();

        var total = await query.CountAsync();
        var users = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = users.Select(u => new ResidentListItemDto(
            Id:          u.Id,
            FullName:    u.FullName,
            Email:       u.Email ?? "",
            PhoneNumber: u.PhoneNumber,
            UserType:    u.UserType.ToString(),
            Units:       u.UserUnits
                          .Where(uu => uu.MoveOutDate == null)
                          .Select(uu => uu.Unit.UnitNumber)
                          .ToArray(),
            NationalId:  u.ResidentProfile?.NationalId,
            CreatedAt:   u.CreatedAt
        )).ToList();

        return Ok(new { total, page, pageSize, items });
    }

    // ── GET /api/residents/search ─────────────────────────────────────────────

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return Ok(new List<ResidentListItemDto>());

        var term = q.Trim().ToLower();
        await using var db = await _factory.CreateDbContextAsync();

        var users = await db.Users
            .Where(u => u.UserType == UserType.HomeOwner || u.UserType == UserType.Resident)
            .Include(u => u.UserUnits).ThenInclude(uu => uu.Unit)
            .Include(u => u.ResidentProfile)
            .Where(u =>
                (u.FirstName + " " + u.LastName).ToLower().Contains(term) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(term)) ||
                (u.ResidentProfile != null && u.ResidentProfile.NationalId != null &&
                 u.ResidentProfile.NationalId.ToLower().Contains(term)))
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .Take(30)
            .ToListAsync();

        return Ok(users.Select(u => new ResidentListItemDto(
            Id:          u.Id,
            FullName:    u.FullName,
            Email:       u.Email ?? "",
            PhoneNumber: u.PhoneNumber,
            UserType:    u.UserType.ToString(),
            Units:       u.UserUnits
                          .Where(uu => uu.MoveOutDate == null)
                          .Select(uu => uu.Unit.UnitNumber)
                          .ToArray(),
            NationalId:  u.ResidentProfile?.NationalId,
            CreatedAt:   u.CreatedAt
        )));
    }

    // ── GET /api/residents/{id} ───────────────────────────────────────────────

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var user = await db.Users
            .Include(u => u.UserUnits).ThenInclude(uu => uu.Unit)
            .Include(u => u.ResidentProfile)
            .Include(u => u.OwnerProfile)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null) return NotFound();

        var vehicles = await db.Vehicles
            .Include(v => v.Tag)
            .Where(v => v.OwnerId == id)
            .OrderBy(v => v.PlateNumber)
            .ToListAsync();

        return Ok(ToDetailDto(user, vehicles));
    }

    // ── POST /api/residents ───────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateResidentRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.LastName))
            return BadRequest(new { error = "First and last name are required." });

        var userType = req.UserType switch
        {
            1 => UserType.HomeOwner,
            2 => UserType.Resident,
            _ => UserType.Resident,
        };

        var user = new ApplicationUser
        {
            TenantId  = _tenantCtx.TenantId,
            FirstName = req.FirstName.Trim(),
            MiddleName = req.MiddleName?.Trim(),
            LastName  = req.LastName.Trim(),
            Email     = req.Email.Trim(),
            UserName  = req.Email.Trim(),
            PhoneNumber = req.PhoneNumber?.Trim(),
            UserType  = userType,
        };

        var result = await _users.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(new { error = string.Join("; ", result.Errors.Select(e => e.Description)) });

        return CreatedAtAction(nameof(GetById), new { id = user.Id },
            new { id = user.Id, fullName = user.FullName, userType = userType.ToString() });
    }

    // ── PUT /api/residents/{id}/profile ───────────────────────────────────────

    [HttpPut("{id}/profile")]
    public async Task<IActionResult> UpsertResidentProfile(string id, [FromBody] UpsertResidentProfileRequest req)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var profile = await db.ResidentProfiles.FirstOrDefaultAsync(p => p.UserId == id);
        if (profile is null)
        {
            profile = new ResidentProfile
            {
                TenantId = _tenantCtx.TenantId,
                UserId   = id,
            };
            db.ResidentProfiles.Add(profile);
        }

        profile.NationalId              = req.NationalId?.Trim();
        profile.PassportNumber          = req.PassportNumber?.Trim();
        profile.DateOfBirth             = req.DateOfBirth;
        profile.Gender                  = req.Gender?.Trim();
        profile.PhysicalAddress         = req.PhysicalAddress?.Trim();
        profile.EmergencyContactName    = req.EmergencyContactName?.Trim();
        profile.EmergencyContactPhone   = req.EmergencyContactPhone?.Trim();
        profile.NextOfKinName           = req.NextOfKinName?.Trim();
        profile.NextOfKinPhone          = req.NextOfKinPhone?.Trim();
        profile.NextOfKinRelationship   = req.NextOfKinRelationship?.Trim();
        profile.UpdatedAt               = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── PUT /api/residents/{id}/owner-profile ─────────────────────────────────

    [HttpPut("{id}/owner-profile")]
    public async Task<IActionResult> UpsertOwnerProfile(string id, [FromBody] UpsertOwnerProfileRequest req)
    {
        await using var db = await _factory.CreateDbContextAsync();

        // Verify the user is actually a HomeOwner
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();
        if (user.UserType != UserType.HomeOwner)
            return BadRequest(new { error = "Owner profile can only be set for HomeOwner accounts." });

        var profile = await db.OwnerProfiles.FirstOrDefaultAsync(p => p.UserId == id);
        if (profile is null)
        {
            profile = new OwnerProfile
            {
                TenantId = _tenantCtx.TenantId,
                UserId   = id,
            };
            db.OwnerProfiles.Add(profile);
        }

        profile.KraPin               = req.KraPin?.Trim();
        profile.BankName             = req.BankName?.Trim();
        profile.BankAccountNumber    = req.BankAccountNumber?.Trim();
        profile.BankBranch           = req.BankBranch?.Trim();
        profile.LevyPaymentMethod    = req.LevyPaymentMethod?.Trim();
        profile.TitleDeedRef         = req.TitleDeedRef?.Trim();
        profile.IsAbsenteeOwner      = req.IsAbsenteeOwner;
        profile.ManagingAgentName    = req.ManagingAgentName?.Trim();
        profile.ManagingAgentContact = req.ManagingAgentContact?.Trim();
        profile.UpdatedAt            = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── PATCH /api/residents/user-units/{userUnitId}/tenancy ──────────────────

    [HttpPatch("user-units/{userUnitId:guid}/tenancy")]
    public async Task<IActionResult> UpdateTenancy(Guid userUnitId, [FromBody] UpdateTenancyRequest req)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var link = await db.UserUnits.FindAsync(userUnitId);
        if (link is null) return NotFound();
        if (link.LinkType != UnitLinkType.Occupant)
            return BadRequest(new { error = "Tenancy details only apply to Occupant links." });

        link.LeaseStartDate     = req.LeaseStartDate;
        link.LeaseEndDate       = req.LeaseEndDate;
        link.MonthlyRent        = req.MonthlyRent;
        link.DepositAmount      = req.DepositAmount;
        link.DepositPaid        = req.DepositPaid;
        link.EmployerName       = req.EmployerName?.Trim();
        link.EmployerPhone      = req.EmployerPhone?.Trim();
        link.GuarantorName      = req.GuarantorName?.Trim();
        link.GuarantorIdNumber  = req.GuarantorIdNumber?.Trim();
        link.GuarantorPhone     = req.GuarantorPhone?.Trim();
        link.RentalAgreementRef = req.RentalAgreementRef?.Trim();

        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ResidentDetailDto ToDetailDto(ApplicationUser u, List<Vehicle> vehicles)
    {
        var rp = u.ResidentProfile;
        var op = u.OwnerProfile;

        return new ResidentDetailDto(
            Id:          u.Id,
            FirstName:   u.FirstName,
            MiddleName:  u.MiddleName,
            LastName:    u.LastName,
            Email:       u.Email ?? "",
            PhoneNumber: u.PhoneNumber,
            UserType:    u.UserType.ToString(),
            CreatedAt:   u.CreatedAt,
            ResidentProfile: rp is null ? null : new ResidentProfileDataDto(
                rp.NationalId, rp.PassportNumber, rp.DateOfBirth, rp.Gender, rp.PhysicalAddress,
                rp.EmergencyContactName, rp.EmergencyContactPhone,
                rp.NextOfKinName, rp.NextOfKinPhone, rp.NextOfKinRelationship),
            OwnerProfile: op is null ? null : new OwnerProfileDataDto(
                op.KraPin, op.BankName, op.BankAccountNumber, op.BankBranch,
                op.LevyPaymentMethod, op.TitleDeedRef, op.IsAbsenteeOwner,
                op.ManagingAgentName, op.ManagingAgentContact),
            Units: u.UserUnits
                .OrderBy(uu => uu.Unit.Block).ThenBy(uu => uu.Unit.UnitNumber)
                .Select(uu => new ResidentUnitLinkDto(
                    UserUnitId:        uu.Id,
                    UnitId:            uu.UnitId,
                    UnitNumber:        uu.Unit.UnitNumber,
                    Block:             uu.Unit.Block,
                    LinkType:          uu.LinkType.ToString(),
                    MoveInDate:        uu.MoveInDate,
                    MoveOutDate:       uu.MoveOutDate,
                    LeaseStartDate:    uu.LeaseStartDate,
                    LeaseEndDate:      uu.LeaseEndDate,
                    MonthlyRent:       uu.MonthlyRent,
                    DepositAmount:     uu.DepositAmount,
                    DepositPaid:       uu.DepositPaid,
                    EmployerName:      uu.EmployerName,
                    EmployerPhone:     uu.EmployerPhone,
                    GuarantorName:     uu.GuarantorName,
                    GuarantorIdNumber: uu.GuarantorIdNumber,
                    GuarantorPhone:    uu.GuarantorPhone,
                    RentalAgreementRef: uu.RentalAgreementRef))
                .ToList(),
            Vehicles: vehicles.Select(v => new ResidentVehicleSummaryDto(
                Id:          v.Id,
                Plate:       v.PlateNumber,
                Make:        v.Make,
                Model:       v.Model,
                Colour:      v.Colour,
                VehicleType: v.Type.ToString(),
                TagNumber:   v.Tag?.TagNumber,
                TagStatus:   v.Tag?.Status.ToString()))
                .ToList()
        );
    }
}
