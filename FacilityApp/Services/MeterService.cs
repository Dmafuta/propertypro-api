using FacilityApp.Data;
using FacilityApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Services;

public class MeterService(AppDbContext context, TenantContext tenantCtx) : IMeterService
{
    // ── Meters ────────────────────────────────────────────────────────────────

    public async Task<List<MeterWithHistory>> GetForUnitAsync(Guid unitId)
    {
        var meters = await context.Meters
            .Where(m => m.UnitId == unitId)
            .OrderByDescending(m => m.IsActive)
            .ThenByDescending(m => m.InstallDate)
            .ToListAsync();

        var result = new List<MeterWithHistory>();
        foreach (var meter in meters)
        {
            var latest = await context.MeterReadings
                .Where(r => r.MeterId == meter.Id)
                .OrderByDescending(r => r.ReadingDate)
                .FirstOrDefaultAsync();
            var count = await context.MeterReadings.CountAsync(r => r.MeterId == meter.Id);
            result.Add(new MeterWithHistory(meter, latest, count));
        }
        return result;
    }

    public async Task<Meter?> GetByIdAsync(Guid meterId) =>
        await context.Meters
            .Include(m => m.Unit)
            .FirstOrDefaultAsync(m => m.Id == meterId);

    public async Task<Meter> AddAsync(Guid unitId, UtilityType utilityType, MeterMode meterMode,
        string meterNumber, string? serialNumber, string? location, string? unitOfMeasure,
        string? metadata, string? notes, DateTime installDate)
    {
        var meter = new Meter
        {
            TenantId      = tenantCtx.TenantId,
            UnitId        = unitId,
            UtilityType   = utilityType,
            MeterMode     = meterMode,
            MeterNumber   = meterNumber.Trim(),
            SerialNumber  = serialNumber?.Trim(),
            Location      = location?.Trim(),
            UnitOfMeasure = unitOfMeasure?.Trim(),
            Metadata      = metadata,
            Notes         = notes?.Trim(),
            InstallDate   = installDate,
            IsActive      = true,
        };
        context.Meters.Add(meter);
        await context.SaveChangesAsync();
        return meter;
    }

    public async Task UpdateAsync(Guid meterId, string meterNumber, string? serialNumber,
        string? location, string? unitOfMeasure, string? metadata, string? notes)
    {
        var meter = await context.Meters.FindAsync(meterId)
            ?? throw new InvalidOperationException("Meter not found.");
        meter.MeterNumber   = meterNumber.Trim();
        meter.SerialNumber  = serialNumber?.Trim();
        meter.Location      = location?.Trim();
        meter.UnitOfMeasure = unitOfMeasure?.Trim();
        meter.Metadata      = metadata;
        meter.Notes         = notes?.Trim();
        await context.SaveChangesAsync();
    }

    public async Task RetireAsync(Guid meterId, decimal closingReadingValue, DateTime retiredAt,
        string? retiredByUserId, string? retirementNotes)
    {
        var meter = await context.Meters.FindAsync(meterId)
            ?? throw new InvalidOperationException("Meter not found.");
        if (!meter.IsActive)
            throw new InvalidOperationException("Meter is already retired.");

        // Log closing reading
        context.MeterReadings.Add(new MeterReading
        {
            TenantId     = tenantCtx.TenantId,
            MeterId      = meterId,
            ReadingValue = closingReadingValue,
            ReadingDate  = retiredAt,
            ReadingType  = MeterReadingType.Closing,
            ReadByUserId = retiredByUserId,
            Notes        = retirementNotes,
            IsVerified   = true,
        });

        meter.IsActive  = false;
        meter.RetiredAt = retiredAt;
        await context.SaveChangesAsync();
    }

    // ── Readings ──────────────────────────────────────────────────────────────

    public async Task<List<MeterReading>> GetReadingsAsync(Guid meterId, int page, int pageSize) =>
        await context.MeterReadings
            .Where(r => r.MeterId == meterId)
            .Include(r => r.ReadBy)
            .OrderByDescending(r => r.ReadingDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<MeterReading> AddReadingAsync(Guid meterId, decimal value, DateTime readingDate,
        MeterReadingType readingType, string? readByUserId, string? photoUrl, string? notes)
    {
        var meter = await context.Meters.FindAsync(meterId)
            ?? throw new InvalidOperationException("Meter not found.");

        var reading = new MeterReading
        {
            TenantId     = tenantCtx.TenantId,
            MeterId      = meterId,
            ReadingValue = value,
            ReadingDate  = readingDate,
            ReadingType  = readingType,
            ReadByUserId = readByUserId,
            PhotoUrl     = photoUrl,
            Notes        = notes,
        };
        context.MeterReadings.Add(reading);
        await context.SaveChangesAsync();
        return reading;
    }

    public async Task VerifyReadingAsync(Guid readingId)
    {
        var reading = await context.MeterReadings.FindAsync(readingId)
            ?? throw new InvalidOperationException("Reading not found.");
        reading.IsVerified = true;
        await context.SaveChangesAsync();
    }

    // ── Prepaid tokens ────────────────────────────────────────────────────────

    public async Task<List<PrepaidToken>> GetTokensAsync(Guid meterId, int page, int pageSize) =>
        await context.PrepaidTokens
            .Where(t => t.MeterId == meterId)
            .Include(t => t.PurchasedBy)
            .OrderByDescending(t => t.PurchasedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

    public async Task<PrepaidToken> AddTokenAsync(Guid meterId, string tokenCode, decimal amountPaid,
        decimal? unitsLoaded, DateTime purchasedAt, DateTime? loadedAt,
        string? purchasedByUserId, string? voucherReference, string? notes)
    {
        var token = new PrepaidToken
        {
            TenantId          = tenantCtx.TenantId,
            MeterId           = meterId,
            TokenCode         = tokenCode.Trim(),
            AmountPaid        = amountPaid,
            UnitsLoaded       = unitsLoaded,
            PurchasedAt       = purchasedAt,
            LoadedAt          = loadedAt,
            PurchasedByUserId = purchasedByUserId,
            VoucherReference  = voucherReference?.Trim(),
            Notes             = notes?.Trim(),
        };
        context.PrepaidTokens.Add(token);
        await context.SaveChangesAsync();
        return token;
    }

    // ── Alerts ────────────────────────────────────────────────────────────────

    public async Task<List<MeterAlert>> GetAlertsAsync(Guid meterId, bool unacknowledgedOnly)
    {
        var q = context.MeterAlerts
            .Where(a => a.MeterId == meterId)
            .Include(a => a.AcknowledgedBy)
            .AsQueryable();
        if (unacknowledgedOnly)
            q = q.Where(a => a.AcknowledgedAt == null);
        return await q.OrderByDescending(a => a.TriggeredAt).ToListAsync();
    }

    public async Task<MeterAlert> CreateAlertAsync(Guid meterId, AlertType alertType,
        AlertSeverity severity, string message, DateTime triggeredAt)
    {
        var alert = new MeterAlert
        {
            TenantId    = tenantCtx.TenantId,
            MeterId     = meterId,
            AlertType   = alertType,
            Severity    = severity,
            Message     = message.Trim(),
            TriggeredAt = triggeredAt,
        };
        context.MeterAlerts.Add(alert);
        await context.SaveChangesAsync();
        return alert;
    }

    public async Task AcknowledgeAlertAsync(Guid alertId, string acknowledgedByUserId)
    {
        var alert = await context.MeterAlerts.FindAsync(alertId)
            ?? throw new InvalidOperationException("Alert not found.");
        alert.AcknowledgedAt          = DateTime.UtcNow;
        alert.AcknowledgedByUserId    = acknowledgedByUserId;
        await context.SaveChangesAsync();
    }

    // ── Installation report ───────────────────────────────────────────────────

    public async Task<MeterInstallationReportData> GetInstallationReportDataAsync(Guid meterId)
    {
        var meter = await context.Meters
            .Include(m => m.Unit)
                .ThenInclude(u => u.UnitType)
            .Include(m => m.Unit)
                .ThenInclude(u => u.UserUnits)
                    .ThenInclude(uu => uu.User)
            .Include(m => m.Unit)
                .ThenInclude(u => u.Tenant)
            .FirstOrDefaultAsync(m => m.Id == meterId)
            ?? throw new InvalidOperationException("Meter not found.");

        var unit    = meter.Unit;
        var tenant  = unit.Tenant;
        var owner   = unit.UserUnits.FirstOrDefault(uu => uu.LinkType == UnitLinkType.Owner)?.User;
        var occupants = unit.UserUnits
            .Where(uu => uu.LinkType == UnitLinkType.Occupant && uu.MoveOutDate == null)
            .Select(uu => uu.User.FullName)
            .ToList();

        var openingReading = await context.MeterReadings
            .Include(r => r.ReadBy)
            .Where(r => r.MeterId == meterId && r.ReadingType == MeterReadingType.Opening)
            .OrderBy(r => r.ReadingDate)
            .FirstOrDefaultAsync();

        return new MeterInstallationReportData(
            TenantName:        tenant.Name,
            TenantLogoUrl:     tenant.LogoUrl,
            TenantAddress:     tenant.Address,
            TenantPhone:       tenant.ContactPhone,
            UnitNumber:        unit.UnitNumber,
            Block:             unit.Block,
            Floor:             unit.Floor,
            UnitTypeName:      unit.UnitType?.Name,
            OwnerName:         owner?.FullName,
            OccupantNames:     occupants.Any() ? string.Join(", ", occupants) : null,
            MeterId:           meter.Id,
            MeterNumber:       meter.MeterNumber,
            SerialNumber:      meter.SerialNumber,
            UtilityType:       meter.UtilityType.ToString(),
            MeterMode:         meter.MeterMode.ToString(),
            Location:          meter.Location,
            UnitOfMeasure:     meter.UnitOfMeasure,
            InstallDate:       meter.InstallDate,
            Notes:             meter.Notes,
            Metadata:          meter.Metadata,
            OpeningReadingValue: openingReading?.ReadingValue,
            OpeningReadingDate:  openingReading?.ReadingDate,
            OpeningReadingBy:    openingReading?.ReadBy?.FullName,
            ReportRef:         $"MIR-{meter.Id.ToString()[..8].ToUpper()}",
            GeneratedAt:       DateTime.UtcNow
        );
    }
}
