using QRCoder;

namespace FacilityApp.Services;

public class QrCodeService : IQrCodeService
{
    public string GenerateVisitQr(Guid visitId)
    {
        using var generator = new QRCodeGenerator();
        using var data      = generator.CreateQrCode(visitId.ToString(), QRCodeGenerator.ECCLevel.M);
        using var qr        = new PngByteQRCode(data);
        var pngBytes = qr.GetGraphic(10);
        return $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";
    }
}
