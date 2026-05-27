namespace FacilityApp.Services;

public class AfricasTalkingSettings
{
    public string Username { get; set; } = "";
    public string ApiKey   { get; set; } = "";
    /// <summary>Optional alphanumeric sender ID or shortcode registered with AT.</summary>
    public string? SenderId { get; set; }
    /// <summary>Set true to route through the AT sandbox (test environment).</summary>
    public bool Sandbox { get; set; } = false;
}

public class SmsService : ISmsService
{
    private const string LiveUrl    = "https://api.africastalking.com/version1/messaging";
    private const string SandboxUrl = "https://api.sandbox.africastalking.com/version1/messaging";

    private readonly AfricasTalkingSettings _at;
    private readonly IHttpClientFactory     _httpFactory;
    private readonly ILogger<SmsService>    _logger;

    public SmsService(AfricasTalkingSettings at, IHttpClientFactory httpFactory, ILogger<SmsService> logger)
    {
        _at          = at;
        _httpFactory = httpFactory;
        _logger      = logger;
    }

    public async Task SendAsync(string to, string message)
    {
        if (string.IsNullOrWhiteSpace(_at.ApiKey))
        {
            _logger.LogWarning("AfricasTalking not configured. Skipping SMS to {To}: {Preview}",
                to, message.Length > 40 ? message[..40] + "..." : message);
            return;
        }

        var endpoint = _at.Sandbox ? SandboxUrl : LiveUrl;

        var form = new Dictionary<string, string>
        {
            ["username"] = _at.Username,
            ["to"]       = to,
            ["message"]  = message
        };
        if (!string.IsNullOrWhiteSpace(_at.SenderId))
            form["from"] = _at.SenderId;

        var client = _httpFactory.CreateClient("africastalking");

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("apiKey", _at.ApiKey);
        request.Headers.Add("Accept", "application/json");
        request.Content = new FormUrlEncodedContent(form);

        try
        {
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("SMS sent to {To}", to);
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("SMS to {To} failed: HTTP {StatusCode} — {Body}", to, (int)response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS to {To} threw an exception", to);
        }
    }

    public Task SendVisitConfirmationAsync(string to, string hostName, string visitorName,
        string purpose, DateTime scheduledAt, string tenantName)
    {
        var when = scheduledAt.ToLocalTime().ToString("ddd d MMM 'at' h:mm tt");
        var msg  = $"{tenantName}: Hi {hostName}, {visitorName} is scheduled to visit you on {when}. Purpose: {purpose}.";
        return SendAsync(to, msg);
    }

    public Task SendCheckInAlertAsync(string to, string hostName, string visitorName,
        string purpose, string tenantName)
    {
        var msg = $"{tenantName}: Hi {hostName}, your visitor {visitorName} has just checked in. Purpose: {purpose}.";
        return SendAsync(to, msg);
    }

    public Task SendParcelArrivedAsync(string to, string recipientName, string description, string tenantName)
    {
        var msg = $"{tenantName}: Hi {recipientName}, a parcel has arrived for you — {description}. Please collect it from reception.";
        return SendAsync(to, msg);
    }

    public Task SendMaintenanceUpdateAsync(string to, string residentName, string title, string status, string tenantName)
    {
        var msg = $"{tenantName}: Hi {residentName}, your maintenance request \"{title}\" has been updated to {status}.";
        return SendAsync(to, msg);
    }

    public Task SendUnitRequestResultAsync(string to, string residentName, string unitNumber, bool approved, string tenantName)
    {
        var msg = approved
            ? $"{tenantName}: Hi {residentName}, your unit request for {unitNumber} has been approved. Welcome home!"
            : $"{tenantName}: Hi {residentName}, your unit request for {unitNumber} was not approved. Please contact management for details.";
        return SendAsync(to, msg);
    }
}
