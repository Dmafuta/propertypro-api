using FacilityApp.Data;
using FacilityApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Services;

public class MaintenanceService : IMaintenanceService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly TenantContext _tenantCtx;
    private readonly IEmailService _email;
    private readonly ISmsService   _sms;

    public MaintenanceService(IDbContextFactory<AppDbContext> factory, TenantContext tenantCtx,
        IEmailService email, ISmsService sms)
    {
        _factory   = factory;
        _tenantCtx = tenantCtx;
        _email     = email;
        _sms       = sms;
    }

    public async Task<List<MaintenanceRequest>> GetForResidentAsync(string residentId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.MaintenanceRequests
            .Include(m => m.Unit)
            .Where(m => m.ResidentId == residentId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<MaintenanceRequest>> GetAllAsync(MaintenanceStatus? status = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.MaintenanceRequests
            .Include(m => m.Resident)
            .Include(m => m.Unit)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(m => m.Status == status.Value);

        return await query.OrderByDescending(m => m.CreatedAt).ToListAsync();
    }

    public async Task<int> GetOpenCountAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.MaintenanceRequests
            .CountAsync(m => m.Status == MaintenanceStatus.Open || m.Status == MaintenanceStatus.InProgress);
    }

    public async Task<MaintenanceRequest> SubmitAsync(string residentId, Guid? unitId, string title, string description, MaintenanceCategory category, MaintenancePriority priority)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var request = new MaintenanceRequest
        {
            TenantId    = _tenantCtx.TenantId,
            ResidentId  = residentId,
            UnitId      = unitId,
            Title       = title.Trim(),
            Description = description.Trim(),
            Category    = category,
            Priority    = priority
        };

        db.MaintenanceRequests.Add(request);
        await db.SaveChangesAsync();
        return request;
    }

    public async Task UpdateStatusAsync(Guid id, MaintenanceStatus status, string? staffNote)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var request = await db.MaintenanceRequests
            .Include(m => m.Resident)
            .FirstOrDefaultAsync(m => m.Id == id)
            ?? throw new InvalidOperationException("Request not found.");

        request.Status    = status;
        request.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(staffNote))
            request.StaffNote = staffNote.Trim();

        if (status == MaintenanceStatus.Resolved || status == MaintenanceStatus.Closed)
            request.ResolvedAt ??= DateTime.UtcNow;

        await db.SaveChangesAsync();

        // Notify the resident
        var resident    = request.Resident;
        var statusLabel = status.ToString();
        var note        = request.StaffNote;

        if (!string.IsNullOrWhiteSpace(resident.Email))
            _ = _email.SendMaintenanceUpdateAsync(resident.Email, resident.FullName,
                request.Title, statusLabel, note, _tenantCtx.TenantName);

        if (!string.IsNullOrWhiteSpace(resident.PhoneNumber))
            _ = _sms.SendMaintenanceUpdateAsync(resident.PhoneNumber, resident.FullName,
                request.Title, statusLabel, _tenantCtx.TenantName);
    }
}
