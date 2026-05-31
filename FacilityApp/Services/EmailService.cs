using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FacilityApp.Services;

public class SmtpSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = false;
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "FacilityApp";
}

public class EmailService : IEmailService
{
    private readonly SmtpSettings _smtp;
    private readonly ILogger<EmailService> _logger;

    public EmailService(SmtpSettings smtp, ILogger<EmailService> logger)
    {
        _smtp   = smtp;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(_smtp.Host))
        {
            _logger.LogWarning("SMTP not configured. Skipping email to {To}: {Subject}", to, subject);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body    = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtp.Host, _smtp.Port,
            _smtp.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable);

        if (!string.IsNullOrWhiteSpace(_smtp.UserName))
            await client.AuthenticateAsync(_smtp.UserName, _smtp.Password);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
        _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
    }

    public Task SendPasswordResetAsync(string to, string recipientName, string resetLink)
    {
        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;">
              <h2 style="color:#1b6ec2;">Password Reset</h2>
              <p>Hi {HtmlEncode(recipientName)},</p>
              <p>We received a request to reset your password. Click the button below to set a new password.</p>
              <p style="margin:24px 0;">
                <a href="{resetLink}" style="background:#1b6ec2;color:#fff;padding:12px 24px;border-radius:4px;text-decoration:none;font-weight:bold;">
                  Reset Password
                </a>
              </p>
              <p style="color:#666;font-size:14px;">This link expires in 2 hours. If you did not request a password reset, you can safely ignore this email.</p>
              <hr style="border:none;border-top:1px solid #eee;margin:24px 0;" />
              <p style="color:#999;font-size:12px;">FacilityApp — Facility Management System</p>
            </div>
            """;
        return SendAsync(to, "Reset your FacilityApp password", html);
    }

    public Task SendVisitConfirmationAsync(string to, string hostName, string visitorName,
        string purpose, DateTime scheduledAt, string tenantName)
    {
        var when = scheduledAt.ToLocalTime().ToString("dddd, MMMM d 'at' h:mm tt");
        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;">
              <h2 style="color:#1b6ec2;">Visit Confirmation</h2>
              <p>Hi {HtmlEncode(hostName)},</p>
              <p>A visit has been pre-registered for you at <strong>{HtmlEncode(tenantName)}</strong>.</p>
              <table style="width:100%;border-collapse:collapse;margin:16px 0;">
                <tr><td style="padding:8px;border-bottom:1px solid #eee;color:#666;width:130px;">Visitor</td>
                    <td style="padding:8px;border-bottom:1px solid #eee;font-weight:bold;">{HtmlEncode(visitorName)}</td></tr>
                <tr><td style="padding:8px;border-bottom:1px solid #eee;color:#666;">Purpose</td>
                    <td style="padding:8px;border-bottom:1px solid #eee;">{HtmlEncode(purpose)}</td></tr>
                <tr><td style="padding:8px;color:#666;">Scheduled</td>
                    <td style="padding:8px;">{when}</td></tr>
              </table>
              <hr style="border:none;border-top:1px solid #eee;margin:24px 0;" />
              <p style="color:#999;font-size:12px;">{HtmlEncode(tenantName)} — Powered by FacilityApp</p>
            </div>
            """;
        return SendAsync(to, $"Upcoming visit: {visitorName}", html);
    }

    public Task SendCheckInAlertAsync(string to, string hostName, string visitorName,
        string purpose, string tenantName)
    {
        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;">
              <h2 style="color:#198754;">Visitor Checked In</h2>
              <p>Hi {HtmlEncode(hostName)},</p>
              <p>Your visitor <strong>{HtmlEncode(visitorName)}</strong> has just checked in at <strong>{HtmlEncode(tenantName)}</strong>.</p>
              <table style="width:100%;border-collapse:collapse;margin:16px 0;">
                <tr><td style="padding:8px;border-bottom:1px solid #eee;color:#666;width:130px;">Purpose</td>
                    <td style="padding:8px;border-bottom:1px solid #eee;">{HtmlEncode(purpose)}</td></tr>
                <tr><td style="padding:8px;color:#666;">Time</td>
                    <td style="padding:8px;">{DateTime.Now:h:mm tt}</td></tr>
              </table>
              <hr style="border:none;border-top:1px solid #eee;margin:24px 0;" />
              <p style="color:#999;font-size:12px;">{HtmlEncode(tenantName)} — Powered by FacilityApp</p>
            </div>
            """;
        return SendAsync(to, $"Your visitor {visitorName} has arrived", html);
    }

    public Task SendParcelArrivedAsync(string to, string recipientName, string description, string tenantName)
    {
        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;">
              <h2 style="color:#1b6ec2;">Parcel Arrived</h2>
              <p>Hi {HtmlEncode(recipientName)},</p>
              <p>A parcel has arrived for you at <strong>{HtmlEncode(tenantName)}</strong>.</p>
              <table style="width:100%;border-collapse:collapse;margin:16px 0;">
                <tr><td style="padding:8px;border-bottom:1px solid #eee;color:#666;width:130px;">Description</td>
                    <td style="padding:8px;border-bottom:1px solid #eee;">{HtmlEncode(description)}</td></tr>
              </table>
              <p>Please collect it from reception or the management office at your earliest convenience.</p>
              <hr style="border:none;border-top:1px solid #eee;margin:24px 0;" />
              <p style="color:#999;font-size:12px;">{HtmlEncode(tenantName)} — Powered by FacilityApp</p>
            </div>
            """;
        return SendAsync(to, $"Parcel arrived for you — {description}", html);
    }

    public Task SendMaintenanceUpdateAsync(string to, string residentName, string title,
        string status, string? staffNote, string tenantName)
    {
        var noteRow = string.IsNullOrWhiteSpace(staffNote) ? "" : $"""
            <tr><td style="padding:8px;color:#666;">Staff note</td>
                <td style="padding:8px;">{HtmlEncode(staffNote)}</td></tr>
            """;
        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;">
              <h2 style="color:#1b6ec2;">Maintenance Request Update</h2>
              <p>Hi {HtmlEncode(residentName)},</p>
              <p>Your maintenance request has been updated at <strong>{HtmlEncode(tenantName)}</strong>.</p>
              <table style="width:100%;border-collapse:collapse;margin:16px 0;">
                <tr><td style="padding:8px;border-bottom:1px solid #eee;color:#666;width:130px;">Request</td>
                    <td style="padding:8px;border-bottom:1px solid #eee;font-weight:bold;">{HtmlEncode(title)}</td></tr>
                <tr><td style="padding:8px;border-bottom:1px solid #eee;color:#666;">New status</td>
                    <td style="padding:8px;border-bottom:1px solid #eee;">{HtmlEncode(status)}</td></tr>
                {noteRow}
              </table>
              <hr style="border:none;border-top:1px solid #eee;margin:24px 0;" />
              <p style="color:#999;font-size:12px;">{HtmlEncode(tenantName)} — Powered by FacilityApp</p>
            </div>
            """;
        return SendAsync(to, $"Maintenance update: {title}", html);
    }

    public Task SendUnitRequestResultAsync(string to, string residentName, string unitNumber,
        bool approved, string? reviewNote, string tenantName)
    {
        var color   = approved ? "#198754" : "#dc3545";
        var heading = approved ? "Unit Request Approved" : "Unit Request Not Approved";
        var body    = approved
            ? $"Your request for unit <strong>{HtmlEncode(unitNumber)}</strong> has been <strong style=\"color:{color}\">approved</strong>. You are now assigned to this unit."
            : $"Your request for unit <strong>{HtmlEncode(unitNumber)}</strong> was not approved at this time.";
        var noteRow = string.IsNullOrWhiteSpace(reviewNote) ? "" : $"""
            <p style="color:#555;margin-top:12px;"><strong>Reason:</strong> {HtmlEncode(reviewNote)}</p>
            """;
        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;">
              <h2 style="color:{color};">{heading}</h2>
              <p>Hi {HtmlEncode(residentName)},</p>
              <p>{body}</p>
              {noteRow}
              <hr style="border:none;border-top:1px solid #eee;margin:24px 0;" />
              <p style="color:#999;font-size:12px;">{HtmlEncode(tenantName)} — Powered by FacilityApp</p>
            </div>
            """;
        var subject = approved ? $"Unit {unitNumber} approved" : $"Unit request update — {unitNumber}";
        return SendAsync(to, subject, html);
    }

    public Task SendAdminInviteAsync(string to, string adminName, string tenantName, string setPasswordLink)
    {
        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;">
              <div style="background:#1b6ec2;padding:32px 32px 24px;border-radius:8px 8px 0 0;text-align:center;">
                <h1 style="color:#fff;margin:0;font-size:24px;font-weight:700;letter-spacing:-0.5px;">Welcome to FacilityApp</h1>
              </div>
              <div style="background:#f9fafb;border:1px solid #e5e7eb;border-top:none;border-radius:0 0 8px 8px;padding:32px;">
                <p style="font-size:16px;color:#111827;margin-top:0;">Hi {HtmlEncode(adminName)},</p>
                <p style="color:#374151;">
                  You've been appointed as the <strong>Administrator</strong> of
                  <strong>{HtmlEncode(tenantName)}</strong> on FacilityApp — a platform that helps
                  facilities manage visitor access, residents, maintenance, parcels, and more.
                </p>
                <p style="color:#374151;">
                  As Administrator, you have full control over your facility: managing staff accounts,
                  configuring settings, overseeing daily operations, and keeping your community running smoothly.
                </p>
                <p style="color:#374151;">To get started, set your password by clicking the button below:</p>
                <p style="text-align:center;margin:32px 0;">
                  <a href="{setPasswordLink}"
                     style="background:#1b6ec2;color:#fff;padding:14px 32px;border-radius:6px;
                            text-decoration:none;font-weight:bold;font-size:15px;display:inline-block;">
                    Set Your Password
                  </a>
                </p>
                <p style="color:#6b7280;font-size:13px;">
                  This link expires in 2 hours. If you weren't expecting this invitation, you can safely ignore this email.
                </p>
                <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0;" />
                <p style="color:#9ca3af;font-size:12px;margin:0;text-align:center;">
                  FacilityApp &mdash; Facility Management System
                </p>
              </div>
            </div>
            """;
        return SendAsync(to, $"You've been invited to manage {tenantName} on FacilityApp", html);
    }

    public Task SendStaffInviteAsync(string to, string name, string tenantName, string setPasswordLink)
    {
        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;">
              <div style="background:#1b6ec2;padding:32px 32px 24px;border-radius:8px 8px 0 0;text-align:center;">
                <h1 style="color:#fff;margin:0;font-size:24px;font-weight:700;letter-spacing:-0.5px;">You've been invited</h1>
              </div>
              <div style="background:#f9fafb;border:1px solid #e5e7eb;border-top:none;border-radius:0 0 8px 8px;padding:32px;">
                <p style="font-size:16px;color:#111827;margin-top:0;">Hi {HtmlEncode(name)},</p>
                <p style="color:#374151;">
                  You've been added as a staff member of <strong>{HtmlEncode(tenantName)}</strong> on FacilityApp.
                  Your account is ready — set your password below to get started.
                </p>
                <p style="text-align:center;margin:32px 0;">
                  <a href="{setPasswordLink}"
                     style="background:#1b6ec2;color:#fff;padding:14px 32px;border-radius:6px;
                            text-decoration:none;font-weight:bold;font-size:15px;display:inline-block;">
                    Set Your Password
                  </a>
                </p>
                <p style="color:#6b7280;font-size:13px;">
                  This link expires in 2 hours. If you weren't expecting this invitation, you can safely ignore this email.
                </p>
                <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0;" />
                <p style="color:#9ca3af;font-size:12px;margin:0;text-align:center;">
                  FacilityApp &mdash; Facility Management System
                </p>
              </div>
            </div>
            """;
        return SendAsync(to, $"You've been invited to {tenantName} on FacilityApp", html);
    }

    public Task SendVisitorBadgeAsync(string to, string visitorName, string purpose,
        string? hostName, DateTime? checkedInAt, string tenantName, string badgeUrl, string? primaryColour)
    {
        var accent   = string.IsNullOrWhiteSpace(primaryColour) ? "#1b6ec2" : primaryColour;
        var timeStr  = checkedInAt.HasValue
            ? checkedInAt.Value.ToLocalTime().ToString("dddd, MMMM d yyyy 'at' h:mm tt")
            : DateTime.UtcNow.ToLocalTime().ToString("dddd, MMMM d yyyy");
        var hostRow  = string.IsNullOrWhiteSpace(hostName) ? "" : $"""
            <tr>
              <td style="padding:10px 16px;border-bottom:1px solid #f0f0f0;color:#666;font-size:14px;width:120px;">Host</td>
              <td style="padding:10px 16px;border-bottom:1px solid #f0f0f0;font-size:14px;font-weight:600;">{HtmlEncode(hostName)}</td>
            </tr>
            """;

        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:560px;margin:0 auto;background:#ffffff;">
              <!-- Header strip -->
              <div style="background:{accent};padding:28px 32px;border-radius:12px 12px 0 0;text-align:center;">
                <div style="display:inline-block;background:rgba(255,255,255,0.15);border:2px solid rgba(255,255,255,0.6);border-radius:6px;padding:4px 16px;margin-bottom:12px;">
                  <span style="color:#fff;font-size:13px;font-weight:700;letter-spacing:3px;text-transform:uppercase;">Visitor Pass</span>
                </div>
                <h1 style="color:#fff;margin:0;font-size:26px;font-weight:700;letter-spacing:-0.5px;">{HtmlEncode(tenantName)}</h1>
              </div>

              <!-- Badge body -->
              <div style="border:1px solid #e5e7eb;border-top:none;border-radius:0 0 12px 12px;padding:32px;">
                <!-- Visitor name -->
                <div style="text-align:center;margin-bottom:24px;">
                  <div style="display:inline-flex;align-items:center;justify-content:center;width:64px;height:64px;border-radius:50%;background:{accent}22;color:{accent};font-size:26px;font-weight:700;margin-bottom:12px;">
                    {HtmlEncode(visitorName[0].ToString().ToUpper())}
                  </div>
                  <h2 style="margin:0 0 4px;font-size:22px;font-weight:700;color:#111827;">{HtmlEncode(visitorName)}</h2>
                  <p style="margin:0;color:#6b7280;font-size:14px;">{timeStr}</p>
                </div>

                <!-- Details table -->
                <table style="width:100%;border-collapse:collapse;background:#f9fafb;border-radius:8px;overflow:hidden;margin-bottom:24px;">
                  <tr>
                    <td style="padding:10px 16px;border-bottom:1px solid #f0f0f0;color:#666;font-size:14px;width:120px;">Purpose</td>
                    <td style="padding:10px 16px;border-bottom:1px solid #f0f0f0;font-size:14px;font-weight:600;">{HtmlEncode(purpose)}</td>
                  </tr>
                  {hostRow}
                  <tr>
                    <td style="padding:10px 16px;color:#666;font-size:14px;">Facility</td>
                    <td style="padding:10px 16px;font-size:14px;font-weight:600;">{HtmlEncode(tenantName)}</td>
                  </tr>
                </table>

                <!-- Badge link CTA -->
                <div style="text-align:center;margin-bottom:24px;">
                  <p style="color:#374151;font-size:14px;margin-bottom:16px;">
                    Show this pass at the gate or tap the button below to open your digital badge.
                  </p>
                  <a href="{badgeUrl}"
                     style="background:{accent};color:#fff;padding:12px 28px;border-radius:6px;
                            text-decoration:none;font-weight:bold;font-size:15px;display:inline-block;">
                    View My Badge
                  </a>
                </div>

                <hr style="border:none;border-top:1px solid #e5e7eb;margin:24px 0 16px;" />
                <p style="color:#9ca3af;font-size:12px;margin:0;text-align:center;">
                  {HtmlEncode(tenantName)} &mdash; Powered by FacilityApp
                </p>
              </div>
            </div>
            """;
        return SendAsync(to, $"Your visitor pass — {tenantName}", html);
    }

    private static string HtmlEncode(string s) =>
        System.Net.WebUtility.HtmlEncode(s);
}
