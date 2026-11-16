using System.IO.Compression;
using ClosedXML.Excel;
using PresensiJamaah.Application.Models;
using PresensiJamaah.Infrastructure.Excel;

namespace PresensiJamaah.Infrastructure.Tests;

public class JamaahExcelTests
{
    private readonly JamaahExcel _excel = new();

    private static readonly string[] Header = ["Nama*", "Nomor HP*", "Alamat*", "Tanggal Lahir*", "Jenis Kelamin*"];

    /// <summary>Membuat workbook dengan header yang benar; isi data lewat <paramref name="isi"/>.</summary>
    private static MemoryStream BuatFile(Action<IXLWorksheet> isi, string sheet = JamaahExcel.SheetData, string[]? header = null)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(sheet);
        var h = header ?? Header;
        for (var i = 0; i < h.Length; i++) ws.Cell(1, i + 1).Value = h[i];
        isi(ws);

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void Template_BerisiSheetDataDanPetunjuk_DenganHeaderBenar()
    {
        using var wb = new XLWorkbook(new MemoryStream(_excel.BuatTemplate()));

        Assert.Equal([JamaahExcel.SheetData, JamaahExcel.SheetPetunjuk], wb.Worksheets.Select(w => w.Name));
        var data = wb.Worksheet(JamaahExcel.SheetData);
        Assert.Equal(Header, Enumerable.Range(1, 5).Select(c => data.Cell(1, c).GetString()));
    }

    [Fact]
    public void Template_KolomHpBerformatTeks_DanJenisKelaminPunyaDropdown()
    {
        var bytes = _excel.BuatTemplate();

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        Assert.Equal("@", wb.Worksheet(JamaahExcel.SheetData).Column(2).Style.NumberFormat.Format);

        using var zip = new ZipArchive(new MemoryStream(bytes));
        var xml = string.Concat(zip.Entries
            .Where(e => e.FullName.StartsWith("xl/worksheets/sheet") && e.FullName.EndsWith(".xml"))
            .Select(e => new StreamReader(e.Open()).ReadToEnd()));

        Assert.Contains("type=\"list\"", xml);
        Assert.Contains("Laki-laki,Perempuan", xml);
    }

    [Fact]
    public void Template_KosongTanpaDataTidakMenghasilkanBaris()
    {
        var rows = _excel.Baca(new MemoryStream(_excel.BuatTemplate()), 1000);

        Assert.Empty(rows);
    }

    [Fact]
    public void Baca_MembacaSemuaKolom_DenganNomorBarisExcel()
    {
        using var file = BuatFile(ws =>
        {
            ws.Cell(2, 1).Value = "Ahmad Fauzi";
            ws.Cell(2, 2).Value = "081234567890";
            ws.Cell(2, 3).Value = "Jl. Mawar 1";
            ws.Cell(2, 4).Value = new DateTime(1990, 5, 17);
            ws.Cell(2, 5).Value = "Laki-laki";
        });

        var row = Assert.Single(_excel.Baca(file, 1000));

        Assert.Equal(2, row.BarisKe);
        Assert.Equal("Ahmad Fauzi", row.Nama);
        Assert.Equal("081234567890", row.NomorHp);
        Assert.Equal("Jl. Mawar 1", row.Alamat);
        Assert.Equal(new DateOnly(1990, 5, 17), row.TanggalLahir);
        Assert.Equal("Laki-laki", row.JenisKelaminTeks);
    }

    [Fact]
    public void Baca_NomorHpTersimpanSebagaiAngka_NolDepanDipulihkan()
    {
        using var file = BuatFile(ws =>
        {
            ws.Cell(2, 1).Value = "Ahmad";
            ws.Cell(2, 2).Value = 81234567890d; // Excel membuang angka 0 di depan
            ws.Cell(3, 1).Value = "Budi";
            ws.Cell(3, 2).Value = 6281234567890d;
        });

        var rows = _excel.Baca(file, 1000);

        Assert.Equal("081234567890", rows[0].NomorHp);
        Assert.Equal("6281234567890", rows[1].NomorHp);
    }

    [Theory]
    [InlineData("17/05/1990")]
    [InlineData("17-05-1990")]
    [InlineData("1990-05-17")]
    [InlineData("17/5/1990")]
    public void Baca_TanggalTeks_DiparseDenganBerbagaiFormat(string teks)
    {
        using var file = BuatFile(ws =>
        {
            ws.Cell(2, 1).Value = "Ahmad";
            ws.Cell(2, 4).Style.NumberFormat.Format = "@";
            ws.Cell(2, 4).Value = teks;
        });

        var row = Assert.Single(_excel.Baca(file, 1000));

        Assert.Equal(new DateOnly(1990, 5, 17), row.TanggalLahir);
    }

    [Fact]
    public void Baca_TanggalTidakValid_TanggalNullDanTeksDipertahankan()
    {
        using var file = BuatFile(ws =>
        {
            ws.Cell(2, 1).Value = "Ahmad";
            ws.Cell(2, 4).Style.NumberFormat.Format = "@";
            ws.Cell(2, 4).Value = "31/02/1990";
        });

        var row = Assert.Single(_excel.Baca(file, 1000));

        Assert.Null(row.TanggalLahir);
        Assert.Equal("31/02/1990", row.TanggalLahirTeks);
    }

    [Fact]
    public void Baca_BarisKosongDilewati_NomorBarisTetapMengikutiExcel()
    {
        using var file = BuatFile(ws =>
        {
            ws.Cell(2, 1).Value = "Ahmad";
            ws.Cell(4, 1).Value = "Budi"; // baris 3 kosong
        });

        var rows = _excel.Baca(file, 1000);

        Assert.Equal([2, 4], rows.Select(r => r.BarisKe));
    }

    [Fact]
    public void Baca_BerhentiSetelahMelewatiBatas()
    {
        using var file = BuatFile(ws =>
        {
            for (var r = 2; r <= 50; r++) ws.Cell(r, 1).Value = $"Jamaah {r}";
        });

        var rows = _excel.Baca(file, 10);

        Assert.Equal(11, rows.Count); // batas + 1 cukup bagi service untuk menolak file
    }

    [Fact]
    public void Baca_SheetDataDipilihBerdasarkanNama_BukanUrutan()
    {
        using var wb = new XLWorkbook();
        wb.Worksheets.Add("Lain").Cell(1, 1).Value = "bukan header";
        var data = wb.Worksheets.Add(JamaahExcel.SheetData);
        for (var i = 0; i < Header.Length; i++) data.Cell(1, i + 1).Value = Header[i];
        data.Cell(2, 1).Value = "Ahmad";
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;

        Assert.Single(_excel.Baca(ms, 1000));
    }

    [Fact]
    public void Baca_HeaderTanpaBintangDanBedaHurufBesarTetapDiterima()
    {
        using var file = BuatFile(
            ws => ws.Cell(2, 1).Value = "Ahmad",
            header: ["nama", "NOMOR HP", "alamat", "tanggal lahir", "jenis kelamin"]);

        Assert.Single(_excel.Baca(file, 1000));
    }

    [Fact]
    public void Baca_HeaderTidakSesuai_Ditolak()
    {
        using var file = BuatFile(
            _ => { },
            header: ["Nama*", "Telepon*", "Alamat*", "Tanggal Lahir*", "Jenis Kelamin*"]);

        var ex = Assert.Throws<ImportFileException>(() => _excel.Baca(file, 1000));
        Assert.Contains("kolom 2", ex.Message);
    }

    [Fact]
    public void Baca_BukanFileXlsx_Ditolak()
    {
        var bukanExcel = new MemoryStream("ini file teks biasa"u8.ToArray());

        Assert.Throws<ImportFileException>(() => _excel.Baca(bukanExcel, 1000));
    }
}
