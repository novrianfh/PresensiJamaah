using PresensiJamaah.Application.Abstractions;
using QRCoder;

namespace PresensiJamaah.Infrastructure.Qr;

public class QrCodeGenerator : IQrCodeGenerator
{
    public byte[] GeneratePng(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Isi QR Code wajib diisi.", nameof(content));

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(pixelsPerModule: 10);
    }
}
