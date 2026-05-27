using FacilityApp.Data;
using FacilityApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Services;

public class ParcelService : IParcelService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly TenantContext _tenantCtx;
    private readonly ISmsService   _sms;
    private readonly IEmailService _email;

    public ParcelService(IDbContextFactory<AppDbContext> factory, TenantContext tenantCtx, ISmsService sms, IEmailService email)
    {
        _factory   = factory;
        _tenantCtx = tenantCtx;
        _sms       = sms;
        _email     = email;
    }

    public async Task<List<Parcel>> GetAllAsync(ParcelStatus? status = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var query = db.Parcels
            .Include(p => p.Unit)
            .Include(p => p.ReceivedBy)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        return await query.OrderByDescending(p => p.ReceivedAt).ToListAsync();
    }

    public async Task<List<Parcel>> GetForUnitAsync(Guid unitId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Parcels
            .Include(p => p.ReceivedBy)
            .Where(p => p.UnitId == unitId)
            .OrderByDescending(p => p.ReceivedAt)
            .ToListAsync();
    }

    public async Task<Parcel> ReceiveAsync(Guid? unitId, string recipientName, string? courierName, string description, string receivedById, string? notes)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var parcel = new Parcel
        {
            TenantId      = _tenantCtx.TenantId,
            UnitId        = unitId,
            RecipientName = recipientName.Trim(),
            CourierName   = string.IsNullOrWhiteSpace(courierName) ? null : courierName.Trim(),
            Description   = description.Trim(),
            ReceivedById  = receivedById,
            Notes         = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
        db.Parcels.Add(parcel);
        await db.SaveChangesAsync();

        // SMS occupants of the unit
        if (unitId.HasValue)
        {
            var occupants = await db.UserUnits
                .Include(uu => uu.User)
                .Where(uu => uu.UnitId == unitId.Value && uu.LinkType == UnitLinkType.Occupant)
                .ToListAsync();

            foreach (var uu in occupants)
            {
                if (!string.IsNullOrWhiteSpace(uu.User.PhoneNumber))
                    _ = _sms.SendParcelArrivedAsync(uu.User.PhoneNumber, recipientName, description, _tenantCtx.TenantName);
                if (!string.IsNullOrWhiteSpace(uu.User.Email))
                    _ = _email.SendParcelArrivedAsync(uu.User.Email, recipientName, description, _tenantCtx.TenantName);
            }
        }

        return parcel;
    }

    public async Task MarkCollectedAsync(Guid id, string collectedByName)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var parcel = await db.Parcels.FindAsync(id)
            ?? throw new InvalidOperationException("Parcel not found.");

        parcel.Status          = ParcelStatus.Collected;
        parcel.CollectedAt     = DateTime.UtcNow;
        parcel.CollectedByName = collectedByName.Trim();
        await db.SaveChangesAsync();
    }

    public async Task MarkReturnedAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var parcel = await db.Parcels.FindAsync(id)
            ?? throw new InvalidOperationException("Parcel not found.");

        parcel.Status = ParcelStatus.Returned;
        await db.SaveChangesAsync();
    }

    public async Task<int> GetPendingCountAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Parcels.CountAsync(p => p.Status == ParcelStatus.Pending);
    }
}
