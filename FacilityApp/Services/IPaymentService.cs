using FacilityApp.Data.Models;

namespace FacilityApp.Services;

public interface IPaymentService
{
    /// <summary>Initiates an M-Pesa STK Push and creates a pending Payment record.</summary>
    Task<(bool Success, Guid? PaymentId, string? CheckoutRequestId, string? Error)>
        InitiateMpesaAsync(
            string phone, decimal amount,
            string? residentId, Guid? unitId,
            PaymentPurpose purpose, string? reference,
            string callbackUrl);

    /// <summary>Processes the Safaricom callback and updates the Payment status.</summary>
    Task HandleMpesaCallbackAsync(Guid tenantId, string checkoutRequestId,
                                  int resultCode, string resultDesc, string? mpesaReceiptNo,
                                  DateTime? transactionDate);

    /// <summary>Records a cash or bank-transfer payment completed outside the system.</summary>
    Task<Guid> RecordManualAsync(
        string? residentId, Guid? unitId, decimal amount,
        PaymentMethod method, PaymentPurpose purpose,
        string? reference, string? notes, string recordedById);

    Task<(List<PaymentListDto> Items, int Total)> GetPagedAsync(
        int page, int pageSize, PaymentStatus? status = null);

    Task<PaymentListDto?> GetByIdAsync(Guid id);
}

public record PaymentListDto(
    Guid      Id,
    string?   ResidentId,
    string?   ResidentName,
    string?   UnitNumber,
    decimal   Amount,
    string    Currency,
    string    Method,
    string    Status,
    string    Purpose,
    string?   PhoneNumber,
    string?   MpesaReceiptNo,
    string?   Reference,
    DateTime  CreatedAt,
    DateTime? PaidAt);
