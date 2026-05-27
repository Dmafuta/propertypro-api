namespace FacilityApp.Services;

public interface ISmsService
{
    Task SendAsync(string to, string message);
    Task SendVisitConfirmationAsync(string to, string hostName, string visitorName,
        string purpose, DateTime scheduledAt, string tenantName);
    Task SendCheckInAlertAsync(string to, string hostName, string visitorName,
        string purpose, string tenantName);
    Task SendParcelArrivedAsync(string to, string recipientName, string description, string tenantName);
    Task SendMaintenanceUpdateAsync(string to, string residentName, string title,
        string status, string tenantName);
    Task SendUnitRequestResultAsync(string to, string residentName, string unitNumber,
        bool approved, string tenantName);
}
