namespace FacilityApp.Services;

public interface IQrCodeService
{
    /// <summary>Returns a base64 data-URI PNG containing a QR code for the given visit ID.</summary>
    string GenerateVisitQr(Guid visitId);
}
