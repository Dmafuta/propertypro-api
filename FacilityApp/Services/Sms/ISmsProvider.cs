namespace FacilityApp.Services.Sms;

/// <summary>
/// Low-level SMS transport abstraction.
/// Each provider (Africa's Talking, Twilio, Vonage, CustomHttp) implements this.
/// </summary>
public interface ISmsProvider
{
    Task SendAsync(string to, string message);
}
