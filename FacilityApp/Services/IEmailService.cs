namespace FacilityApp.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody);
    Task SendPasswordResetAsync(string to, string recipientName, string resetLink);
    Task SendVisitConfirmationAsync(string to, string hostName, string visitorName,
        string purpose, DateTime scheduledAt, string tenantName);
    Task SendCheckInAlertAsync(string to, string hostName, string visitorName,
        string purpose, string tenantName);
    Task SendParcelArrivedAsync(string to, string recipientName, string description, string tenantName);
    Task SendMaintenanceUpdateAsync(string to, string residentName, string title,
        string status, string? staffNote, string tenantName);
    Task SendUnitRequestResultAsync(string to, string residentName, string unitNumber,
        bool approved, string? reviewNote, string tenantName);
    Task SendAdminInviteAsync(string to, string adminName, string tenantName, string setPasswordLink);
    Task SendStaffInviteAsync(string to, string name, string tenantName, string setPasswordLink);
}
