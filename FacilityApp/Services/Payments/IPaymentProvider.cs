namespace FacilityApp.Services.Payments;

public record StkPushResult(
    bool    Success,
    string? CheckoutRequestId,
    string? MerchantRequestId,
    string? CustomerMessage,
    string? Error);

public interface IPaymentProvider
{
    Task<StkPushResult> StkPushAsync(
        string phone, decimal amount,
        string accountRef, string description,
        string callbackUrl);
}
