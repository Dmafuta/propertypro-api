using FacilityApp.Data.Models;

namespace FacilityApp.Services;

public record MeterWithHistory(Meter Meter, MeterReading? LatestReading, int ReadingCount);

public interface IMeterService
{
    // Meters
    Task<List<MeterWithHistory>> GetForUnitAsync(Guid unitId);
    Task<Meter?> GetByIdAsync(Guid meterId);
    Task<Meter> AddAsync(Guid unitId, UtilityType utilityType, MeterMode meterMode,
        string meterNumber, string? serialNumber, string? location, string? unitOfMeasure,
        string? metadata, string? notes, DateTime installDate);
    Task UpdateAsync(Guid meterId, string meterNumber, string? serialNumber,
        string? location, string? unitOfMeasure, string? metadata, string? notes);
    Task RetireAsync(Guid meterId, decimal closingReadingValue, DateTime retiredAt,
        string? retiredByUserId, string? retirementNotes);

    // Readings
    Task<List<MeterReading>> GetReadingsAsync(Guid meterId, int page, int pageSize);
    Task<MeterReading> AddReadingAsync(Guid meterId, decimal value, DateTime readingDate,
        MeterReadingType readingType, string? readByUserId, string? photoUrl, string? notes);
    Task VerifyReadingAsync(Guid readingId);

    // Prepaid tokens
    Task<List<PrepaidToken>> GetTokensAsync(Guid meterId, int page, int pageSize);
    Task<PrepaidToken> AddTokenAsync(Guid meterId, string tokenCode, decimal amountPaid,
        decimal? unitsLoaded, DateTime purchasedAt, DateTime? loadedAt,
        string? purchasedByUserId, string? voucherReference, string? notes);

    // Alerts
    Task<List<MeterAlert>> GetAlertsAsync(Guid meterId, bool unacknowledgedOnly);
    Task<MeterAlert> CreateAlertAsync(Guid meterId, AlertType alertType, AlertSeverity severity,
        string message, DateTime triggeredAt);
    Task AcknowledgeAlertAsync(Guid alertId, string acknowledgedByUserId);

    // Report data
    Task<MeterInstallationReportData> GetInstallationReportDataAsync(Guid meterId);
}

public record MeterInstallationReportData(
    // Tenant
    string TenantName, string? TenantLogoUrl, string? TenantAddress, string? TenantPhone,
    // Unit
    string UnitNumber, string? Block, string? Floor, string? UnitTypeName,
    string? OwnerName, string? OccupantNames,
    // Meter
    Guid MeterId, string MeterNumber, string? SerialNumber, string UtilityType, string MeterMode,
    string? Location, string? UnitOfMeasure, DateTime InstallDate, string? Notes, string? Metadata,
    // Opening reading
    decimal? OpeningReadingValue, DateTime? OpeningReadingDate, string? OpeningReadingBy,
    // Report meta
    string ReportRef, DateTime GeneratedAt
);
