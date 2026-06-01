using FacilityApp.Data;
using FacilityApp.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FacilityApp.Services;

public class SettingsService : ISettingsService
{
    private readonly AppDbContext _context;
    private readonly TenantContext _tenantCtx;

    public SettingsService(AppDbContext context, TenantContext tenantCtx)
    {
        _context   = context;
        _tenantCtx = tenantCtx;
    }

    public async Task<Tenant?> GetAsync()
        => await _context.Tenants.FindAsync(_tenantCtx.TenantId);

    public async Task UpdateAsync(string name, string? contactEmail, string? contactPhone, string? address, string? website, string? customDomain)
    {
        var tenant = await _context.Tenants.FindAsync(_tenantCtx.TenantId)
            ?? throw new InvalidOperationException("Tenant not found.");

        if (!string.IsNullOrWhiteSpace(customDomain) && tenant.Plan != Data.Models.TenantPlan.Professional)
            throw new InvalidOperationException("Custom domain is a Professional plan feature. Please upgrade to enable it.");

        tenant.Name         = name.Trim();
        tenant.ContactEmail = contactEmail?.Trim();
        tenant.ContactPhone = contactPhone?.Trim();
        tenant.Address      = address?.Trim();
        tenant.Website      = website?.Trim();
        tenant.CustomDomain = string.IsNullOrWhiteSpace(customDomain) ? null : customDomain.Trim().ToLower();
        await _context.SaveChangesAsync();

        _tenantCtx.TenantName = tenant.Name;
    }

    public async Task UpdateBrandingAsync(string? logoUrl, string? primaryColour)
    {
        var tenant = await _context.Tenants.FindAsync(_tenantCtx.TenantId)
            ?? throw new InvalidOperationException("Tenant not found.");
        tenant.LogoUrl       = logoUrl?.Trim();
        tenant.PrimaryColour = primaryColour?.Trim();
        await _context.SaveChangesAsync();

        _tenantCtx.LogoUrl       = tenant.LogoUrl;
        _tenantCtx.PrimaryColour = tenant.PrimaryColour;
    }

    public async Task UpdateSmsAsync(bool enabled, int provider, string? apiKey, string? username, string? senderId, string? apiUrl)
    {
        var tenant = await _context.Tenants.FindAsync(_tenantCtx.TenantId)
            ?? throw new InvalidOperationException("Tenant not found.");

        // Custom API credentials are Professional-only
        if (!string.IsNullOrWhiteSpace(apiKey) && tenant.Plan != Data.Models.TenantPlan.Professional)
            throw new InvalidOperationException("Custom SMS credentials are a Professional plan feature. Please upgrade to enable them.");

        tenant.SmsEnabled   = enabled;
        tenant.SmsProvider  = (Data.Models.SmsProvider)provider;
        tenant.SmsApiKey    = string.IsNullOrWhiteSpace(apiKey)   ? null : apiKey.Trim();
        tenant.SmsUsername  = string.IsNullOrWhiteSpace(username)  ? null : username.Trim();
        tenant.SmsSenderId  = string.IsNullOrWhiteSpace(senderId)  ? null : senderId.Trim();
        tenant.SmsApiUrl    = string.IsNullOrWhiteSpace(apiUrl)    ? null : apiUrl.Trim();
        await _context.SaveChangesAsync();

        _tenantCtx.SmsEnabled   = tenant.SmsEnabled;
        _tenantCtx.SmsProvider  = tenant.SmsProvider;
        _tenantCtx.SmsApiKey    = tenant.SmsApiKey;
        _tenantCtx.SmsUsername  = tenant.SmsUsername;
        _tenantCtx.SmsSenderId  = tenant.SmsSenderId;
        _tenantCtx.SmsApiUrl    = tenant.SmsApiUrl;
    }

    public async Task UpdateMpesaAsync(
        bool enabled, bool sandbox,
        string? shortCode, string? consumerKey, string? consumerSecret, string? passkey)
    {
        var tenant = await _context.Tenants.FindAsync(_tenantCtx.TenantId)
            ?? throw new InvalidOperationException("Tenant not found.");

        if (enabled && tenant.Plan != Data.Models.TenantPlan.Professional)
            throw new InvalidOperationException("M-Pesa integration is a Professional plan feature. Please upgrade to enable it.");

        tenant.MpesaEnabled        = enabled;
        tenant.MpesaSandbox        = sandbox;
        tenant.MpesaShortCode      = string.IsNullOrWhiteSpace(shortCode)      ? null : shortCode.Trim();
        tenant.MpesaConsumerKey    = string.IsNullOrWhiteSpace(consumerKey)    ? null : consumerKey.Trim();
        tenant.MpesaConsumerSecret = string.IsNullOrWhiteSpace(consumerSecret) ? null : consumerSecret.Trim();
        tenant.MpesaPasskey        = string.IsNullOrWhiteSpace(passkey)        ? null : passkey.Trim();
        await _context.SaveChangesAsync();

        _tenantCtx.MpesaEnabled        = tenant.MpesaEnabled;
        _tenantCtx.MpesaSandbox        = tenant.MpesaSandbox;
        _tenantCtx.MpesaShortCode      = tenant.MpesaShortCode;
        _tenantCtx.MpesaConsumerKey    = tenant.MpesaConsumerKey;
        _tenantCtx.MpesaConsumerSecret = tenant.MpesaConsumerSecret;
        _tenantCtx.MpesaPasskey        = tenant.MpesaPasskey;
    }
}
