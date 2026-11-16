using Microsoft.Extensions.Time.Testing;
using PresensiJamaah.Application.Abstractions;
using PresensiJamaah.Application.Models;
using PresensiJamaah.Application.Services;
using PresensiJamaah.Domain.Entities;
using PresensiJamaah.Domain.Enums;

namespace PresensiJamaah.Application.Tests;

public class JamaahImportServiceTests
{
    private sealed class StubExcel(IReadOnlyList<JamaahImportRow>? rows = null) : IJamaahExcel
    {
        public byte[] BuatTemplate() => [1, 2, 3];
        public IReadOnlyList<JamaahImportRow> Baca(Stream stream, int maxBaris) => rows ?? [];
    }

    private static readonly FakeTimeProvider Time = new(new DateTimeOffset(2026, 11, 4, 10, 0, 0, TimeSpan.Zero));

    private static JamaahImportService Buat(FakeJamaahRepository repo) => new(repo, new StubExcel(), Time);

    private static JamaahImportRow Baris(
        int baris = 2,
        string? nama = "Ahmad Fauzi",
        string? hp = "081234567890",
        string? alamat = "Jl. Mawar 1",
        DateOnly? tanggal = null,
        string? jk = "Laki-laki") =>
        new(baris, nama, hp, alamat, tanggal ?? new DateOnly(1990, 5, 17), null, jk);

    [Fact]
    public async Task BarisValid_DisimpanDenganNomorBerurutan_DalamSatuKaliSimpan()
    {
        var repo = new FakeJamaahRepository();

        var hasil = await Buat(repo).ImportRowsAsync([
            Baris(2, "Ahmad", "081111111111"),
            Baris(3, "Siti", "082222222222", jk: "Perempuan")]);

        Assert.True(hasil.Berhasil);
        Assert.Equal(2, hasil.Diimpor);
        Assert.Equal(["J-000001", "J-000002"], repo.Data.Select(j => j.NomorJamaah));
        Assert.Equal(JenisKelamin.Perempuan, repo.Data[1].JenisKelamin);
        Assert.Equal(1, repo.SaveCount);
    }

    [Fact]
    public async Task SatuBarisSalah_SemuaDibatalkan_TidakAdaYangDisimpan()
    {
        var repo = new FakeJamaahRepository();

        var hasil = await Buat(repo).ImportRowsAsync([
            Baris(2),
            Baris(3, nama: "", hp: "abc", jk: "xyz")]);

        Assert.False(hasil.Berhasil);
        Assert.Equal(0, hasil.Diimpor);
        Assert.Empty(repo.Data);
        Assert.Equal(0, repo.SaveCount);

        var salah = Assert.Single(hasil.Kesalahan);
        Assert.Equal(3, salah.BarisKe);
        Assert.Contains("Nama wajib diisi", salah.Pesan);
        Assert.Contains("Nomor HP", salah.Pesan);
        Assert.Contains("Jenis kelamin", salah.Pesan);
    }

    [Fact]
    public async Task SemuaBarisSalahDilaporkan_BukanHanyaYangPertama()
    {
        var repo = new FakeJamaahRepository();

        var hasil = await Buat(repo).ImportRowsAsync([
            Baris(2, nama: null),
            Baris(3),
            Baris(4, alamat: " ")]);

        Assert.Equal([2, 4], hasil.Kesalahan.Select(k => k.BarisKe));
    }

    [Fact]
    public async Task TanggalLahir_MasaDepanDanTidakTerbaca_Ditolak()
    {
        var repo = new FakeJamaahRepository();

        var hasil = await Buat(repo).ImportRowsAsync([
            Baris(2, tanggal: new DateOnly(2027, 1, 1)),
            new JamaahImportRow(3, "Budi", "081234567890", "Alamat", null, "31/02/2000", "L"),
            new JamaahImportRow(4, "Cici", "081234567890", "Alamat", null, null, "P")]);

        Assert.Contains("masa depan", hasil.Kesalahan.Single(k => k.BarisKe == 2).Pesan);
        Assert.Contains("tidak terbaca", hasil.Kesalahan.Single(k => k.BarisKe == 3).Pesan);
        Assert.Contains("wajib diisi", hasil.Kesalahan.Single(k => k.BarisKe == 4).Pesan);
    }

    [Theory]
    [InlineData("Laki-laki", JenisKelamin.LakiLaki)]
    [InlineData("laki laki", JenisKelamin.LakiLaki)]
    [InlineData("L", JenisKelamin.LakiLaki)]
    [InlineData("Pria", JenisKelamin.LakiLaki)]
    [InlineData("Perempuan", JenisKelamin.Perempuan)]
    [InlineData(" p ", JenisKelamin.Perempuan)]
    [InlineData("Wanita", JenisKelamin.Perempuan)]
    public async Task JenisKelamin_BerbagaiPenulisanDikenali(string teks, JenisKelamin diharapkan)
    {
        var repo = new FakeJamaahRepository();

        var hasil = await Buat(repo).ImportRowsAsync([Baris(jk: teks)]);

        Assert.True(hasil.Berhasil);
        Assert.Equal(diharapkan, repo.Data.Single().JenisKelamin);
    }

    [Fact]
    public async Task NomorHp_DinormalkanSebelumDisimpan()
    {
        var repo = new FakeJamaahRepository();

        await Buat(repo).ImportRowsAsync([Baris(hp: "0812-3456 7890")]);

        Assert.Equal("081234567890", repo.Data.Single().NomorHp);
    }

    [Fact]
    public async Task Duplikat_DenganDataYangSudahAda_Dilewati()
    {
        var repo = new FakeJamaahRepository();
        var service = Buat(repo);
        await service.ImportRowsAsync([Baris(2, "Ahmad Fauzi", "081234567890")]);

        // Nama beda huruf besar/kecil dan awalan HP berbeda (+62) tetap dianggap sama.
        var hasil = await service.ImportRowsAsync([
            Baris(2, "AHMAD FAUZI", "+6281234567890"),
            Baris(3, "Siti", "082222222222")]);

        Assert.True(hasil.Berhasil);
        Assert.Equal(1, hasil.Diimpor);
        Assert.Equal(2, repo.Data.Count);
        Assert.Equal(2, Assert.Single(hasil.Dilewati).BarisKe);
    }

    [Fact]
    public async Task Duplikat_DiDalamFileYangSama_Dilewati()
    {
        var repo = new FakeJamaahRepository();

        var hasil = await Buat(repo).ImportRowsAsync([
            Baris(2, "Ahmad", "081234567890"),
            Baris(3, "Ahmad", "081234567890")]);

        Assert.Equal(1, hasil.Diimpor);
        Assert.Equal(3, Assert.Single(hasil.Dilewati).BarisKe);
    }

    [Fact]
    public async Task NamaSamaTapiHpBeda_BukanDuplikat()
    {
        var repo = new FakeJamaahRepository();

        var hasil = await Buat(repo).ImportRowsAsync([
            Baris(2, "Ahmad", "081111111111"),
            Baris(3, "Ahmad", "082222222222")]);

        Assert.Equal(2, hasil.Diimpor);
        Assert.Empty(hasil.Dilewati);
    }

    [Fact]
    public async Task UnggahUlangFileYangSama_AmanTidakMenggandakan()
    {
        var repo = new FakeJamaahRepository();
        var service = Buat(repo);
        var rows = new[] { Baris(2, "Ahmad", "081111111111"), Baris(3, "Siti", "082222222222") };

        await service.ImportRowsAsync(rows);
        var kedua = await service.ImportRowsAsync(rows);

        Assert.Equal(0, kedua.Diimpor);
        Assert.Equal(2, kedua.Dilewati.Count);
        Assert.Equal(2, repo.Data.Count);
    }

    [Fact]
    public async Task FileKosong_Gagal()
    {
        var hasil = await Buat(new FakeJamaahRepository()).ImportRowsAsync([]);

        Assert.False(hasil.Berhasil);
        Assert.Contains("tidak berisi data", hasil.Kesalahan.Single().Pesan);
    }

    [Fact]
    public async Task MelebihiBatasBaris_Gagal()
    {
        var repo = new FakeJamaahRepository();
        var rows = Enumerable.Range(2, JamaahImportService.MaxBaris + 1)
            .Select(i => Baris(i, $"Jamaah {i}", $"08123{i:D7}"))
            .ToList();

        var hasil = await Buat(repo).ImportRowsAsync(rows);

        Assert.False(hasil.Berhasil);
        Assert.Empty(repo.Data);
    }

    [Fact]
    public async Task ImportAsync_MembacaDariExcelLaluMemproses()
    {
        var repo = new FakeJamaahRepository();
        var service = new JamaahImportService(repo, new StubExcel([Baris()]), Time);

        var hasil = await service.ImportAsync(new MemoryStream());

        Assert.Equal(1, hasil.Diimpor);
    }

    [Fact]
    public void BuatTemplate_MeneruskanDariExcel()
    {
        Assert.Equal([1, 2, 3], Buat(new FakeJamaahRepository()).BuatTemplate());
    }
}
