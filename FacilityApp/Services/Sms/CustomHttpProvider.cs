using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace FacilityApp.Services.Sms;

/// <summary>
/// Generic HTTP POST SMS provider.
/// Posts JSON { "to": "...", "message": "...", "from": "..." } to the configured URL.
/// Optional Bearer token auth via ApiKey.
/// Suitable for any custom gateway that accepts a simple JSON webhook.
/// </summary>
public class CustomHttpProvider : ISmsProvider
{
    private readonly string  _apiUrl;
    private readonly string? _apiKey;
    private readonly string? _from;
    private readonly IHttpClientFactory       _httpFactory;
    private readonly ILogger<CustomHttpProvider> _logger;

    public CustomHttpProvider(
        string apiUrl, string? apiKey, string? from,
        IHttpClientFactory httpFactory,
        ILogger<CustomHttpProvider> logger)
    {
        _apiUrl      = apiUrl;
        _apiKey      = apiKey;
        _from        = from;
        _httpFactory = httpFactory;
        _logger      = logger;
    }

    public async Task SendAsync(string to, string message)
    {
        var payload = new { to, message, from = _from };
        var json    = JsonSerializer.Serialize(payload);

        var client = _httpFactory.CreateClient("customsms");
        using var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        if (!string.IsNullOrWhiteSpace(_apiKey))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

        try
        {
            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
                _logger.LogInformation("CustomHttp SMS sent to {To}: {Body}", to, body);
            else
                _logger.LogError("CustomHttp SMS to {To} failed: HTTP {StatusCode} — {Body}", to, (int)response.StatusCode, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CustomHttp SMS to {To} threw an exception", to);
        }
    }
}
