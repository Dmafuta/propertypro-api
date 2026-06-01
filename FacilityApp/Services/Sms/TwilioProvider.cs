using System.Text;
using Microsoft.Extensions.Logging;

namespace FacilityApp.Services.Sms;

/// <summary>
/// SMS provider adapter for Twilio.
/// Credentials: Username = Account SID, ApiKey = Auth Token, SenderId = From phone number (e.g. +12345678900).
/// Uses Twilio REST API — no SDK required.
/// </summary>
public class TwilioProvider : ISmsProvider
{
    private readonly string  _accountSid;
    private readonly string  _authToken;
    private readonly string  _from;
    private readonly IHttpClientFactory    _httpFactory;
    private readonly ILogger<TwilioProvider> _logger;

    public TwilioProvider(
        string accountSid, string authToken, string from,
        IHttpClientFactory httpFactory,
        ILogger<TwilioProvider> logger)
    {
        _accountSid  = accountSid;
        _authToken   = authToken;
        _from        = from;
        _httpFactory = httpFactory;
        _logger      = logger;
    }

    public async Task SendAsync(string to, string message)
    {
        var url = $"https://api.twilio.com/2010-04-01/Accounts/{_accountSid}/Messages.json";
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_accountSid}:{_authToken}"));

        var form = new Dictionary<string, string>
        {
            ["From"] = _from,
            ["To"]   = to,
            ["Body"] = message,
        };

        var client = _httpFactory.CreateClient("twilio");
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(form);

        try
        {
            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
                _logger.LogInformation("Twilio SMS sent to {To}: {Body}", to, body);
            else
                _logger.LogError("Twilio SMS to {To} failed: HTTP {StatusCode} — {Body}", to, (int)response.StatusCode, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Twilio SMS to {To} threw an exception", to);
        }
    }
}
