using Microsoft.Extensions.Logging;

namespace FacilityApp.Services.Sms;

/// <summary>Platform-level Africa's Talking credentials from appsettings.</summary>
public class AfricasTalkingSettings
{
    public string  ApiKey   { get; set; } = string.Empty;
    public string  Username { get; set; } = string.Empty;
    public string? SenderId { get; set; }
    public bool    Sandbox  { get; set; } = false;
}

/// <summary>
/// SMS provider adapter for Africa's Talking.
/// Credentials: ApiKey = AT API Key, Username = AT Username, SenderId = registered sender ID.
/// </summary>
public class AfricasTalkingProvider : ISmsProvider
{
    private const string LiveUrl    = "https://api.africastalking.com/version1/messaging";
    private const string SandboxUrl = "https://api.sandbox.africastalking.com/version1/messaging";

    private readonly string  _apiKey;
    private readonly string  _username;
    private readonly string? _senderId;
    private readonly bool    _sandbox;
    private readonly IHttpClientFactory         _httpFactory;
    private readonly ILogger<AfricasTalkingProvider> _logger;

    public AfricasTalkingProvider(
        string apiKey, string username, string? senderId, bool sandbox,
        IHttpClientFactory httpFactory,
        ILogger<AfricasTalkingProvider> logger)
    {
        _apiKey      = apiKey;
        _username    = username;
        _senderId    = senderId;
        _sandbox     = sandbox;
        _httpFactory = httpFactory;
        _logger      = logger;
    }

    public async Task SendAsync(string to, string message)
    {
        var endpoint = _sandbox ? SandboxUrl : LiveUrl;

        var form = new Dictionary<string, string>
        {
            ["username"] = _username,
            ["to"]       = to,
            ["message"]  = message,
        };
        if (!string.IsNullOrWhiteSpace(_senderId))
            form["from"] = _senderId;

        var client = _httpFactory.CreateClient("africastalking");
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("apiKey", _apiKey);
        request.Headers.Add("Accept", "application/json");
        request.Content = new FormUrlEncodedContent(form);

        try
        {
            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
                _logger.LogInformation("AT SMS sent to {To}: {Body}", to, body);
            else
                _logger.LogError("AT SMS to {To} failed: HTTP {StatusCode} — {Body}", to, (int)response.StatusCode, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AT SMS to {To} threw an exception", to);
        }
    }
}
