using QRCoder;

namespace MyPersonalWebsite.Services.BoardGame;

public sealed class QrCodeService
{
    public string GeneratePngBase64(string text, int pixelsPerModule = 10)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        var bytes = new PngByteQRCode(data).GetGraphic(pixelsPerModule);
        return "data:image/png;base64," + Convert.ToBase64String(bytes);
    }
}
