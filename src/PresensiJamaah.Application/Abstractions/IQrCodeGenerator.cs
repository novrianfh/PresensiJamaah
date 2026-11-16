namespace PresensiJamaah.Application.Abstractions;

public interface IQrCodeGenerator
{
    /// <summary>Membuat gambar QR Code (PNG) berisi <paramref name="content"/>.</summary>
    byte[] GeneratePng(string content);
}
