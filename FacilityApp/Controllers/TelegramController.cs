using System.Security.Claims;
using System.Text.Json;
using FacilityApp.Data;
using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FacilityApp.Controllers;

/// <summary>
/// Handles the Telegram Bot webhook and the resident linking flow.
/// </summary>
[ApiController]
[Route("api/telegram")]
public class TelegramController : ControllerBase
{
    private readonly AppDbContext        _context;
    private readonly TenantContext       _tenantCtx;
    private readonly ITelegramService    _telegram;
    private readonly IMemoryCache        _cache;
    private readonly ILogger<TelegramController> _logger;

    private static readonly TimeSpan LinkTokenTtl = TimeSpan.FromMinutes(15);

    public TelegramController(AppDbContext context, TenantContext tenantCtx,
        ITelegramService telegram, IMemoryCache cache,
        ILogger<TelegramController> logger)
    {
        _context   = context;
        _tenantCtx = tenantCtx;
        _telegram  = telegram;
        _cache     = cache;
        _logger    = logger;
    }

    // ─── POST /api/telegram/webhook ─────────────────────────────────────────────
    // Called by Telegram's servers — no authentication (verified by checking bot token in URL)
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(
        [FromQuery(Name = "token")] string? urlToken,
        [FromBody] JsonElement update)
    {
        // Validate the URL token matches a known tenant bot token
        if (string.IsNullOrWhiteSpace(urlToken))
            return Forbid();

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.TelegramEnabled && t.TelegramBotToken == urlToken);

        if (tenant is null)
        {
            _logger.LogWarning("Telegram webhook received with unknown token.");
            return Ok(); // always 200 to Telegram
        }

        try
        {
            if (!update.TryGetProperty("message", out var message))
                return Ok();

            if (!message.TryGetProperty("text", out var textEl) ||
                !message.TryGetProperty("chat", out var chat)   ||
                !chat.TryGetProperty("id", out var chatIdEl))
                return Ok();

            var text   = textEl.GetString() ?? "";
            var chatId = chatIdEl.GetInt64();

            // Handle /start {linkToken}
            if (text.StartsWith("/start "))
            {
                var linkToken = text["/start ".Length..].Trim();
                if (_cache.TryGetValue($"tg_link:{linkToken}", out string? userId) && userId is not null)
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user is not null && user.TenantId == tenant.Id)
                    {
                        user.TelegramChatId = chatId;
                        await _context.SaveChangesAsync();
                        _cache.Remove($"tg_link:{linkToken}");

                        await _telegram.SendAsync(chatId,
                            $"<b>Linked!</b> Your Telegram account is now connected to {tenant.Name}. You will receive notifications here.");
                    }
                    else
                    {
                        await _telegram.SendAsync(chatId,
                            "This link has expired or is invalid. Please generate a new one from the app.");
                    }
                }
                else
                {
                    await _telegram.SendAsync(chatId,
                        "This link has expired or is invalid. Please generate a new one from the app.");
                }
            }
            else if (text == "/start")
            {
                await _telegram.SendAsync(chatId,
                    $"Welcome to {tenant.Name}! To link your account, tap <b>Link Telegram</b> in the app and follow the instructions.");
            }
            else if (text == "/unlink")
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.TelegramChatId == chatId && u.TenantId == tenant.Id);
                if (user is not null)
                {
                    user.TelegramChatId = null;
                    await _context.SaveChangesAsync();
                    await _telegram.SendAsync(chatId,
                        "Your Telegram account has been unlinked. You will no longer receive notifications here.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Telegram webhook update.");
        }

        return Ok();
    }

    // ─── POST /api/telegram/link ─────────────────────────────────────────────────
    // Authenticated — generates a one-time link token for the current user
    [HttpPost("link")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public IActionResult GenerateLinkToken()
    {
        if (!_tenantCtx.TelegramEnabled || string.IsNullOrWhiteSpace(_tenantCtx.TelegramBotToken))
            return BadRequest(new { error = "Telegram is not configured for this facility." });

        var userId    = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var linkToken = Guid.NewGuid().ToString("N");
        _cache.Set($"tg_link:{linkToken}", userId, LinkTokenTtl);

        return Ok(new { linkToken, expiresInMinutes = (int)LinkTokenTtl.TotalMinutes });
    }

    // ─── DELETE /api/telegram/link ───────────────────────────────────────────────
    // Removes the TelegramChatId from the current user
    [HttpDelete("link")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Unlink()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user is null) return NotFound();

        user.TelegramChatId = null;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // ─── GET /api/telegram/link/status ───────────────────────────────────────────
    [HttpGet("link/status")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> LinkStatus()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var user = await _context.Users.FindAsync(userId);
        if (user is null) return NotFound();

        return Ok(new { linked = user.TelegramChatId is not null });
    }
}
