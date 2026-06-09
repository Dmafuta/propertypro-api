using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace FacilityApp.Services;

/// <summary>
/// Sends Telegram messages via the Bot API using the per-tenant bot token.
/// </summary>
public class TelegramService : ITelegramService
{
    private readonly IHttpClientFactory     _httpFactory;
    private readonly TenantContext          _tenantCtx;
    private readonly ILogger<TelegramService> _logger;

    public TelegramService(IHttpClientFactory httpFactory, TenantContext tenantCtx,
        ILogger<TelegramService> logger)
    {
        _httpFactory = httpFactory;
        _tenantCtx   = tenantCtx;
        _logger      = logger;
    }

    public async Task SendAsync(long chatId, string message)
    {
        if (!_tenantCtx.TelegramEnabled || string.IsNullOrWhiteSpace(_tenantCtx.TelegramBotToken))
        {
            _logger.LogDebug("Telegram disabled or not configured for tenant {Slug}. Skipping.", _tenantCtx.TenantSlug);
            return;
        }

        try
        {
            var client = _httpFactory.CreateClient("telegram");
            var url    = $"https://api.telegram.org/bot{_tenantCtx.TelegramBotToken}/sendMessage";
            var payload = new { chat_id = chatId, text = message, parse_mode = "HTML" };
            var response = await client.PostAsJsonAsync(url, payload);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Telegram sendMessage failed for chatId {ChatId}: {Status} {Body}",
                    chatId, response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telegram sendMessage threw for chatId {ChatId}", chatId);
        }
    }

    public Task SendVisitConfirmationAsync(long? chatId, string hostName, string visitorName,
        string purpose, DateTime scheduledAt, string tenantName)
    {
        if (chatId is null) return Task.CompletedTask;
        var when = scheduledAt.ToLocalTime().ToString("ddd d MMM 'at' h:mm tt");
        var msg  = $"<b>{tenantName}</b>\nHi {hostName}, <b>{visitorName}</b> is scheduled to visit you on {when}.\nPurpose: {purpose}";
        return SendAsync(chatId.Value, msg);
    }

    public Task SendCheckInAlertAsync(long? chatId, string hostName, string visitorName,
        string purpose, string tenantName)
    {
        if (chatId is null) return Task.CompletedTask;
        var msg = $"<b>{tenantName}</b>\nHi {hostName}, your visitor <b>{visitorName}</b> has just checked in.\nPurpose: {purpose}";
        return SendAsync(chatId.Value, msg);
    }

    public Task SendParcelArrivedAsync(long? chatId, string recipientName, string description, string tenantName)
    {
        if (chatId is null) return Task.CompletedTask;
        var msg = $"<b>{tenantName}</b>\nHi {recipientName}, a parcel has arrived for you — {description}.\nPlease collect it from reception.";
        return SendAsync(chatId.Value, msg);
    }

    public Task SendMaintenanceUpdateAsync(long? chatId, string residentName, string title,
        string status, string tenantName)
    {
        if (chatId is null) return Task.CompletedTask;
        var msg = $"<b>{tenantName}</b>\nHi {residentName}, your maintenance request <b>\"{title}\"</b> has been updated to {status}.";
        return SendAsync(chatId.Value, msg);
    }
}
