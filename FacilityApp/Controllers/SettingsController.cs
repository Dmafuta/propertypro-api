using FacilityApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FacilityApp.Controllers;

[ApiController]
[Route("api/settings")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settings;

    public SettingsController(ISettingsService settings) => _settings = settings;

    // GET /api/settings
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var tenant = await _settings.GetAsync();
        if (tenant is null) return NotFound();
        return Ok(new
        {
            tenant.Id, tenant.Name, tenant.Slug, tenant.Plan,
            tenant.ContactEmail, tenant.ContactPhone,
            tenant.Address, tenant.Website, tenant.CustomDomain,
            tenant.PrimaryColour, tenant.LogoUrl,
            tenant.SmsEnabled, tenant.SmsProvider, tenant.SmsApiKey, tenant.SmsUsername, tenant.SmsSenderId, tenant.SmsApiUrl,
            tenant.TelegramEnabled, tenant.TelegramBotToken,
            tenant.MpesaEnabled, tenant.MpesaSandbox, tenant.MpesaShortCode,
            tenant.MpesaConsumerKey, tenant.MpesaConsumerSecret, tenant.MpesaPasskey
        });
    }

    // PUT /api/settings
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateSettingsRequest req)
    {
        try
        {
            await _settings.UpdateAsync(
                req.Name, req.ContactEmail, req.ContactPhone,
                req.Address, req.Website, req.CustomDomain);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // PATCH /api/settings/branding
    [HttpPatch("branding")]
    public async Task<IActionResult> UpdateBranding([FromBody] UpdateBrandingRequest req)
    {
        await _settings.UpdateBrandingAsync(req.LogoUrl, req.PrimaryColour);
        return NoContent();
    }

    // PATCH /api/settings/mpesa
    [HttpPatch("mpesa")]
    public async Task<IActionResult> UpdateMpesa([FromBody] UpdateMpesaRequest req)
    {
        try
        {
            await _settings.UpdateMpesaAsync(
                req.Enabled, req.Sandbox,
                req.ShortCode, req.ConsumerKey, req.ConsumerSecret, req.Passkey);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // PATCH /api/settings/sms
    [HttpPatch("sms")]
    public async Task<IActionResult> UpdateSms([FromBody] UpdateSmsRequest req)
    {
        try
        {
            await _settings.UpdateSmsAsync(req.Enabled, req.Provider, req.ApiKey, req.Username, req.SenderId, req.ApiUrl);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // PATCH /api/settings/telegram
    [HttpPatch("telegram")]
    public async Task<IActionResult> UpdateTelegram([FromBody] UpdateTelegramRequest req)
    {
        try
        {
            await _settings.UpdateTelegramAsync(req.Enabled, req.BotToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
