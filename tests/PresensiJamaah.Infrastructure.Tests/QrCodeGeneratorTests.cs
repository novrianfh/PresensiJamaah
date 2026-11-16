using PresensiJamaah.Infrastructure.Qr;

namespace PresensiJamaah.Infrastructure.Tests;

public class QrCodeGeneratorTests
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Fact]
    public void GeneratePng_NomorJamaahValid_MenghasilkanPngTidakKosong()
    {
        var png = new QrCodeGenerator().GeneratePng("J-000001");

        Assert.True(png.Length > 100);
        Assert.Equal(PngSignature, png.Take(8).ToArray());
    }

    [Fact]
    public void GeneratePng_IsiBerbeda_MenghasilkanGambarBerbeda()
    {
        var generator = new QrCodeGenerator();

        Assert.NotEqual(generator.GeneratePng("J-000001"), generator.GeneratePng("J-000002"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GeneratePng_IsiKosong_Melempar(string content)
    {
        Assert.Throws<ArgumentException>(() => new QrCodeGenerator().GeneratePng(content));
    }
}
