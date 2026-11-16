using ClosedXML.Excel;
using PresensiJamaah.Application.Models;
using PresensiJamaah.Infrastructure.Excel;

namespace PresensiJamaah.Infrastructure.Tests;

public class RekapExcelTests
{
    private static RekapResult Contoh(params RekapRow[] rows) => new(
        RekapPeriode.Bulan, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30),
        null, null, 10, 50m, rows);

    private static XLWorkbook Buka(byte[] bytes) => new(new MemoryStream(bytes));

    [Fact]
    public void Buat_MenulisHeaderDanBarisData()
    {
        var bytes = new RekapExcel().Buat(Contoh(
            new RekapRow(Guid.NewGuid(), "J-000001", "Ahmad", 8, 80m, true),
            new RekapRow(Guid.NewGuid(), "J-000002", "Siti", 2, 20m, false)));

        using var wb = Buka(bytes);
        var ws = wb.Worksheet(RekapExcel.SheetName);

        Assert.Equal(
            ["No", "Nomor Jamaah", "Nama", "Hadir", "Total Kejadian", "Persentase (%)", "Status"],
            Enumerable.Range(1, 7).Select(c => ws.Cell(RekapExcel.HeaderRow, c).GetString()));

        var r1 = RekapExcel.HeaderRow + 1;
        Assert.Equal(1, ws.Cell(r1, 1).GetDouble());
        Assert.Equal("J-000001", ws.Cell(r1, 2).GetString());
        Assert.Equal("Ahmad", ws.Cell(r1, 3).GetString());
        Assert.Equal(8, ws.Cell(r1, 4).GetDouble());
        Assert.Equal(10, ws.Cell(r1, 5).GetDouble());
        Assert.Equal(80, ws.Cell(r1, 6).GetDouble());
        Assert.Equal("Aktif", ws.Cell(r1, 7).GetString());

        Assert.Equal("Siti", ws.Cell(r1 + 1, 3).GetString());
        Assert.Equal("Tidak aktif", ws.Cell(r1 + 1, 7).GetString());
    }

    [Fact]
    public void Buat_MenuliskanPeriodeEventDanRingkasan()
    {
        var rekap = Contoh(new RekapRow(Guid.NewGuid(), "J-000001", "Ahmad", 8, 80m, true)) with { EventNama = "Maghrib" };

        using var wb = Buka(new RekapExcel().Buat(rekap));
        var ws = wb.Worksheet(RekapExcel.SheetName);

        Assert.Contains("01/11/2026", ws.Cell(2, 2).GetString());
        Assert.Contains("30/11/2026", ws.Cell(2, 2).GetString());
        Assert.Equal("Maghrib", ws.Cell(3, 2).GetString());
        Assert.Contains("1 dari 1 jamaah aktif", ws.Cell(4, 2).GetString());
    }

    [Fact]
    public void Buat_TanpaFilterEvent_MenulisSemuaEvent()
    {
        using var wb = Buka(new RekapExcel().Buat(Contoh()));

        Assert.Equal("Semua event", wb.Worksheet(RekapExcel.SheetName).Cell(3, 2).GetString());
    }

    [Fact]
    public void Buat_TanpaJamaah_TetapMenghasilkanFileValid()
    {
        using var wb = Buka(new RekapExcel().Buat(Contoh()));
        var ws = wb.Worksheet(RekapExcel.SheetName);

        Assert.Equal("No", ws.Cell(RekapExcel.HeaderRow, 1).GetString());
        Assert.True(ws.Cell(RekapExcel.HeaderRow + 1, 1).IsEmpty());
    }

    [Theory]
    [InlineData("=1+1")]
    [InlineData("+SUM(A1:A2)")]
    [InlineData("@cmd")]
    public void Buat_NamaBerawalanKarakterRumus_TersimpanSebagaiTeksBukanRumus(string nama)
    {
        var bytes = new RekapExcel().Buat(Contoh(new RekapRow(Guid.NewGuid(), "J-000001", nama, 1, 10m, false)));

        using var wb = Buka(bytes);
        var cell = wb.Worksheet(RekapExcel.SheetName).Cell(RekapExcel.HeaderRow + 1, 3);

        Assert.False(cell.HasFormula);
        Assert.Equal(XLDataType.Text, cell.DataType);
        Assert.Equal(nama, cell.GetString());
    }
}
