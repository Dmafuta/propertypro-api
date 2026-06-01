using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FacilityApp.Services.Payments;

/// <summary>
/// Safaricom Daraja M-Pesa provider.
/// Handles OAuth token caching (55-min TTL) + STK Push initiation.
/// Credentials: ConsumerKey, ConsumerSecret, ShortCode, Passkey.
/// </summary>
public class MpesaProvider : IPaymentProvider
{
    private const string SandboxBase = "https://sandbox.safaricom.co.ke";
    private const string LiveBase    = "https://api.safaricom.co.ke";

    private readonly string  _consumerKey;
    private readonly string  _consumerSecret;
    private readonly string  _shortCode;
    private readonly string  _passkey;
    private readonly bool    _sandbox;
    private readonly IHttpClientFactory      _httpFactory;
    private readonly IMemoryCache            _cache;
    private readonly ILogger<MpesaProvider>  _logger;

    private string BaseUrl       => _sandbox ? SandboxBase : LiveBase;
    private string TokenCacheKey => $"mpesa_token_{_shortCode}";

    public MpesaProvider(
        string consumerKey, string consumerSecret,
        string shortCode,   string passkey, bool sandbox,
        IHttpClientFactory httpFactory,
        IMemoryCache cache,
        ILogger<MpesaProvider> logger)
    {
        _consumerKey    = consumerKey;
        _consumerSecret = consumerSecret;
        _shortCode      = shortCode;
        _passkey        = passkey;
        _sandbox        = sandbox;
        _httpFactory    = httpFactory;
        _cache          = cache;
        _logger         = logger;
    }

    // ── Phone normalization ────────────────────────────────────────────────────
    private static string NormalizePhone(string phone)
    {
        phone = phone.Trim().Replace(" ", "").Replace("-", "");
        if (phone.StartsWith("+"))  phone = phone[1..];
        if (phone.StartsWith("07") || phone.StartsWith("01"))
            phone = "254" + phone[1..];
        return phone;
    }

    // ── OAuth token (cached 55 min) ────────────────────────────────────────────
    private async Task<string?> GetTokenAsync()
    {
        if (_cache.TryGetValue(TokenCacheKey, out string? cached))
            return cached;

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_consumerKey}:{_consumerSecret}"));

        var client = _httpFactory.CreateClient("mpesa");
        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            $"{BaseUrl}/oauth/v1/generate?grant_type=client_credentials");
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        try
        {
            var resp = await client.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("M-Pesa OAuth failed: HTTP {Code}", (int)resp.StatusCode);
                return null;
            }

            using var doc  = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
            var token = doc.RootElement.GetProperty("access_token").GetString();
            _cache.Set(TokenCacheKey, token,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(55)
                });
            return token;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "M-Pesa OAuth threw an exception");
            return null;
        }
    }

    // ── STK Push ──────────────────────────────────────────────────────────────
    public async Task<StkPushResult> StkPushAsync(
        string phone, decimal amount,
        string accountRef, string description,
        string callbackUrl)
    {
        var token = await GetTokenAsync();
        if (token is null)
            return new StkPushResult(false, null, null, null,
                "Failed to obtain M-Pesa access token.");

        var normalizedPhone = NormalizePhone(phone);
        var timestamp       = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var passwordRaw     = $"{_shortCode}{_passkey}{timestamp}";
        var password        = Convert.ToBase64String(Encoding.UTF8.GetBytes(passwordRaw));

        // Daraja character limits
        var safeRef  = accountRef.Length  > 12 ? accountRef[..12]  : accountRef;
        var safeDesc = description.Length > 13 ? description[..13] : description;

        var payload = new
        {
            BusinessShortCode = _shortCode,
            Password          = password,
            Timestamp         = timestamp,
            TransactionType   = "CustomerPayBillOnline",
            Amount            = (int)Math.Ceiling(amount),
            PartyA            = normalizedPhone,
            PartyB            = _shortCode,
            PhoneNumber       = normalizedPhone,
            CallBackURL       = callbackUrl,
            AccountReference  = safeRef,
            TransactionDesc   = safeDesc,
        };

        var client = _httpFactory.CreateClient("mpesa");
        using var req = new HttpRequestMessage(
            HttpMethod.Post,
            $"{BaseUrl}/mpesa/stkpush/v1/processrequest");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(payload);

        try
        {
            var resp = await client.SendAsync(req);
            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
            var root = doc.RootElement;

            var responseCode = root.TryGetProperty("ResponseCode",       out var rc) ? rc.GetString()  : null;
            var checkoutId   = root.TryGetProperty("CheckoutRequestID",  out var ci) ? ci.GetString()  : null;
            var merchantId   = root.TryGetProperty("MerchantRequestID",  out var mi) ? mi.GetString()  : null;
            var message      = root.TryGetProperty("CustomerMessage",    out var cm) ? cm.GetString()  : null;

            if (responseCode == "0")
            {
                _logger.LogInformation(
                    "M-Pesa STK Push to {Phone} — CheckoutRequestID: {Id}", phone, checkoutId);
                return new StkPushResult(true, checkoutId, merchantId, message, null);
            }

            var err = root.TryGetProperty("errorMessage", out var em) ? em.GetString() : "STK push failed.";
            _logger.LogError("M-Pesa STK Push failed: {Err}", err);
            return new StkPushResult(false, null, null, null, err);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "M-Pesa STK Push threw an exception");
            return new StkPushResult(false, null, null, null, ex.Message);
        }
    }
}
