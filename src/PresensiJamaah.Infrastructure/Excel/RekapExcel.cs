using System.Globalization;
using ClosedXML.Excel;
using PresensiJamaah.Application.Abstractions;
using PresensiJamaah.Application.Models;

namespace PresensiJamaah.Infrastructure.Excel;

public class RekapExcel : IRekapExcel
{
    public const string SheetName = "Rekap";
    public const int HeaderRow = 6;

    private static readonly string[] Headers =
        ["No", "Nomor Jamaah", "Nama", "Hadir", "Total Kejadian", "Persentase (%)", "Status"];

    private static readonly CultureInfo Id = CultureInfo.GetCultureInfo("id-ID");

    public byte[] Buat(RekapResult rekap)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(SheetName);

        // Semua teks ditulis lewat SetText agar isi seperti "=1+1" tidak pernah dianggap rumus.
        SetText(ws.Cell(1, 1), "Rekap Kehadiran Jamaah");
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;

        SetText(ws.Cell(2, 1), "Periode");
        SetText(ws.Cell(2, 2), $"{rekap.Periode} ({rekap.Dari:dd/MM/yyyy} – {rekap.Sampai:dd/MM/yyyy})");
        SetText(ws.Cell(3, 1), "Event");
        SetText(ws.Cell(3, 2), rekap.EventNama ?? "Semua event");
        SetText(ws.Cell(4, 1), "Batas jamaah aktif");
        SetText(ws.Cell(4, 2), $"{rekap.MinimalPersen.ToString("0.##", Id)}% dari {rekap.TotalKejadian} kejadian · {rekap.JumlahAktif} dari {rekap.Rows.Count} jamaah aktif");
        ws.Range(2, 1, 4, 1).Style.Font.Bold = true;

        for (var i = 0; i < Headers.Length; i++)
        {
            var cell = ws.Cell(HeaderRow, i + 1);
            SetText(cell, Headers[i]);
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCE9F7");
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }

        var row = HeaderRow + 1;
        var no = 1;
        foreach (var r in rekap.Rows)
        {
            ws.Cell(row, 1).Value = no++;
            SetText(ws.Cell(row, 2), r.NomorJamaah);
            SetText(ws.Cell(row, 3), r.Nama);
            ws.Cell(row, 4).Value = r.Hadir;
            ws.Cell(row, 5).Value = rekap.TotalKejadian;
            ws.Cell(row, 6).Value = Math.Round(r.Persen, 1);
            SetText(ws.Cell(row, 7), r.Aktif ? "Aktif" : "Tidak aktif");
            row++;
        }

        if (rekap.Rows.Count > 0)
            ws.Range(HeaderRow + 1, 6, row - 1, 6).Style.NumberFormat.Format = "0.0";

        ws.Column(1).Width = 18;
        ws.Column(2).Width = 16;
        ws.Column(3).Width = 32;
        ws.Column(4).Width = 8;
        ws.Column(5).Width = 16;
        ws.Column(6).Width = 16;
        ws.Column(7).Width = 14;
        ws.SheetView.FreezeRows(HeaderRow);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static void SetText(IXLCell cell, string text)
    {
        cell.SetValue(text);
        cell.Style.NumberFormat.Format = "@";
    }
}
