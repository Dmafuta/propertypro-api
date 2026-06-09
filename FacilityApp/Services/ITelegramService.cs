namespace FacilityApp.Services;

public interface ITelegramService
{
    Task SendAsync(long chatId, string message);
    Task SendVisitConfirmationAsync(long? chatId, string hostName, string visitorName,
        string purpose, DateTime scheduledAt, string tenantName);
    Task SendCheckInAlertAsync(long? chatId, string hostName, string visitorName,
        string purpose, string tenantName);
    Task SendParcelArrivedAsync(long? chatId, string recipientName, string description, string tenantName);
    Task SendMaintenanceUpdateAsync(long? chatId, string residentName, string title,
        string status, string tenantName);
}
