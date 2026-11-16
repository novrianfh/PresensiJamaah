using PresensiJamaah.Domain.Entities;
using PresensiJamaah.Domain.Enums;

namespace PresensiJamaah.Domain.Tests;

public class JamaahTests
{
    private static Jamaah Buat() => Jamaah.Create(
        "J-000001", "  Ahmad  ", "081234567890", "Jl. Mawar 1", new DateOnly(1990, 5, 17), JenisKelamin.LakiLaki);

    [Fact]
    public void Create_MengisiDataDanMemangkasSpasi_DanAktifSecaraDefault()
    {
        var j = Buat();

        Assert.NotEqual(Guid.Empty, j.Id);
        Assert.Equal("Ahmad", j.Nama);
        Assert.Equal("J-000001", j.NomorJamaah);
        Assert.True(j.IsActive);
    }

    [Theory]
    [InlineData("", "081", "alamat")]
    [InlineData("   ", "081", "alamat")]
    [InlineData("Ahmad", "", "alamat")]
    [InlineData("Ahmad", "081", " ")]
    public void Create_DataWajibKosong_Ditolak(string nama, string hp, string alamat)
    {
        Assert.Throws<ArgumentException>(() => Jamaah.Create(
            "J-000001", nama, hp, alamat, new DateOnly(1990, 1, 1), JenisKelamin.Perempuan));
    }

    [Fact]
    public void Create_NamaTerlaluPanjang_Ditolak()
    {
        var nama = new string('a', Jamaah.NamaMaxLength + 1);
        Assert.Throws<ArgumentException>(() => Jamaah.Create(
            "J-000001", nama, "081", "alamat", new DateOnly(1990, 1, 1), JenisKelamin.LakiLaki));
    }

    [Fact]
    public void Create_JenisKelaminTidakValid_Ditolak()
    {
        Assert.Throws<ArgumentException>(() => Jamaah.Create(
            "J-000001", "Ahmad", "081", "alamat", new DateOnly(1990, 1, 1), (JenisKelamin)99));
    }

    [Fact]
    public void Update_TidakMengubahNomorJamaah()
    {
        var j = Buat();

        j.Update("Budi", "082222222222", "Jl. Melati 2", new DateOnly(1985, 3, 2), JenisKelamin.LakiLaki);

        Assert.Equal("Budi", j.Nama);
        Assert.Equal("J-000001", j.NomorJamaah);
    }

    [Fact]
    public void NonaktifkanDanAktifkan_MengubahStatus()
    {
        var j = Buat();

        j.Nonaktifkan();
        Assert.False(j.IsActive);

        j.Aktifkan();
        Assert.True(j.IsActive);
    }

    [Theory]
    [InlineData(1, "J-000001")]
    [InlineData(42, "J-000042")]
    [InlineData(1234567, "J-1234567")]
    public void FormatNomorJamaah_MemakaiEnamDigit(long urutan, string diharapkan)
    {
        Assert.Equal(diharapkan, Jamaah.FormatNomorJamaah(urutan));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void FormatNomorJamaah_UrutanTidakPositif_Ditolak(long urutan)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Jamaah.FormatNomorJamaah(urutan));
    }
}
