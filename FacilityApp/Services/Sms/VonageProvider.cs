using Microsoft.Extensions.Logging;

namespace FacilityApp.Services.Sms;

/// <summary>
/// SMS provider adapter for Vonage (formerly Nexmo).
/// Credentials: ApiKey = Vonage API Key, Username = Vonage API Secret, SenderId = From name/number.
/// </summary>
public class VonageProvider : ISmsProvider
{
    private const string Url = "https://rest.nexmo.com/sms/json";

    private readonly string  _apiKey;
    private readonly string  _apiSecret;
    private readonly string  _from;
    private readonly IHttpClientFactory   _httpFactory;
    private readonly ILogger<VonageProvider> _logger;

    public VonageProvider(
        string apiKey, string apiSecret, string from,
        IHttpClientFactory httpFactory,
        ILogger<VonageProvider> logger)
    {
        _apiKey     = apiKey;
        _apiSecret  = apiSecret;
        _from       = from;
        _httpFactory = httpFactory;
        _logger     = logger;
    }

    public async Task SendAsync(string to, string message)
    {
        var form = new Dictionary<string, string>
        {
            ["api_key"]    = _apiKey,
            ["api_secret"] = _apiSecret,
            ["from"]       = _from,
            ["to"]         = to,
            ["text"]       = message,
        };

        var client = _httpFactory.CreateClient("vonage");
        using var request = new HttpRequestMessage(HttpMethod.Post, Url);
        request.Headers.Add("Accept", "application/json");
        request.Content = new FormUrlEncodedContent(form);

        try
        {
            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
                _logger.LogInformation("Vonage SMS sent to {To}: {Body}", to, body);
            else
                _logger.LogError("Vonage SMS to {To} failed: HTTP {StatusCode} — {Body}", to, (int)response.StatusCode, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vonage SMS to {To} threw an exception", to);
        }
    }
}
