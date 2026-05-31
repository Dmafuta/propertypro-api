using FacilityApp.Data;
using FacilityApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Services;

public class UnitRequestService : IUnitRequestService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly IUnitService  _unitSvc;
    private readonly TenantContext _tenantCtx;
    private readonly IEmailService _email;
    private readonly ISmsService   _sms;

    public UnitRequestService(IDbContextFactory<AppDbContext> factory, IUnitService unitSvc,
        TenantContext tenantCtx, IEmailService email, ISmsService sms)
    {
        _factory   = factory;
        _unitSvc   = unitSvc;
        _tenantCtx = tenantCtx;
        _email     = email;
        _sms       = sms;
    }

    public async Task<UnitRequest?> GetForResidentAsync(string residentId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.UnitRequests
            .Include(r => r.Unit)
            .Include(r => r.ReviewedBy)
            .Where(r => r.ResidentId == residentId)
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<UnitRequest>> GetPendingAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.UnitRequests
            .Include(r => r.Unit)
            .Include(r => r.Resident)
            .Where(r => r.Status == UnitRequestStatus.Pending)
            .OrderBy(r => r.RequestedAt)
            .ToListAsync();
    }

    public async Task<int> GetPendingCountAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.UnitRequests
            .CountAsync(r => r.Status == UnitRequestStatus.Pending);
    }

    public async Task<UnitRequest> SubmitAsync(string residentId, Guid unitId, string? note)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var hasPending = await db.UnitRequests
            .AnyAsync(r => r.ResidentId == residentId && r.Status == UnitRequestStatus.Pending);

        if (hasPending)
            throw new InvalidOperationException("You already have a pending unit request.");

        var request = new UnitRequest
        {
            TenantId   = _tenantCtx.TenantId,
            ResidentId = residentId,
            UnitId     = unitId,
            Note       = note?.Trim()
        };

        db.UnitRequests.Add(request);
        await db.SaveChangesAsync();
        return request;
    }

    public async Task ApproveAsync(Guid requestId, string reviewerId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var request = await db.UnitRequests
            .Include(r => r.Resident)
            .Include(r => r.Unit)
            .FirstOrDefaultAsync(r => r.Id == requestId)
            ?? throw new InvalidOperationException("Request not found.");

        if (request.Status != UnitRequestStatus.Pending)
            throw new InvalidOperationException("Request is no longer pending.");

        // Assign the unit occupant (this also grants the Occupant role)
        await _unitSvc.AddOccupantAsync(request.UnitId, request.ResidentId, null);

        request.Status       = UnitRequestStatus.Approved;
        request.ReviewedById = reviewerId;
        request.ReviewedAt   = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var resident   = request.Resident;
        var unitNumber = request.Unit.UnitNumber;

        if (!string.IsNullOrWhiteSpace(resident.Email))
            _ = _email.SendUnitRequestResultAsync(resident.Email, resident.FullName,
                unitNumber, approved: true, reviewNote: null, _tenantCtx.TenantName);

        if (!string.IsNullOrWhiteSpace(resident.PhoneNumber))
            _ = _sms.SendUnitRequestResultAsync(resident.PhoneNumber, resident.FullName,
                unitNumber, approved: true, _tenantCtx.TenantName);
    }

    public async Task RejectAsync(Guid requestId, string reviewerId, string? reviewNote)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var request = await db.UnitRequests
            .Include(r => r.Resident)
            .Include(r => r.Unit)
            .FirstOrDefaultAsync(r => r.Id == requestId)
            ?? throw new InvalidOperationException("Request not found.");

        if (request.Status != UnitRequestStatus.Pending)
            throw new InvalidOperationException("Request is no longer pending.");

        request.Status       = UnitRequestStatus.Rejected;
        request.ReviewedById = reviewerId;
        request.ReviewNote   = reviewNote?.Trim();
        request.ReviewedAt   = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var resident   = request.Resident;
        var unitNumber = request.Unit.UnitNumber;

        if (!string.IsNullOrWhiteSpace(resident.Email))
            _ = _email.SendUnitRequestResultAsync(resident.Email, resident.FullName,
                unitNumber, approved: false, request.ReviewNote, _tenantCtx.TenantName);

        if (!string.IsNullOrWhiteSpace(resident.PhoneNumber))
            _ = _sms.SendUnitRequestResultAsync(resident.PhoneNumber, resident.FullName,
                unitNumber, approved: false, _tenantCtx.TenantName);
    }
}
