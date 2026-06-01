namespace FacilityApp.Data.Models;

public class Payment
{
    public Guid    Id       { get; set; } = Guid.NewGuid();
    public Guid    TenantId { get; set; }

    // Payer — optional so cash payments can be logged without a resident account
    public string? ResidentId { get; set; }
    public Guid?   UnitId     { get; set; }

    public decimal Amount   { get; set; }
    public string  Currency { get; set; } = "KES";

    public PaymentMethod  Method  { get; set; }
    public PaymentStatus  Status  { get; set; } = PaymentStatus.Pending;
    public PaymentPurpose Purpose { get; set; }

    // M-Pesa / gateway fields
    public string? PhoneNumber       { get; set; }
    public string? MpesaReceiptNo    { get; set; }
    public string? CheckoutRequestId { get; set; }
    public string? MerchantRequestId { get; set; }
    public string? ResultDescription { get; set; }

    // General
    public string? Reference    { get; set; }
    public string? Notes        { get; set; }
    public string? RecordedById { get; set; }   // staff who logged a cash/manual payment

    public DateTime  CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt    { get; set; }
    public DateTime  UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ApplicationUser? Resident   { get; set; }
    public Unit?            Unit       { get; set; }
    public ApplicationUser? RecordedBy { get; set; }
}

public enum PaymentMethod  { Mpesa = 0, Cash = 1, BankTransfer = 2, Card = 3 }
public enum PaymentStatus  { Pending = 0, Processing = 1, Completed = 2, Failed = 3, Cancelled = 4 }
public enum PaymentPurpose { Rent = 0, Levy = 1, Deposit = 2, Utility = 3, Facility = 4, Other = 5 }
