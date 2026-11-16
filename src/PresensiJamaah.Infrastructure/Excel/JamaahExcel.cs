using System.Globalization;
using ClosedXML.Excel;
using PresensiJamaah.Application.Abstractions;
using PresensiJamaah.Application.Models;
using PresensiJamaah.Application.Services;

namespace PresensiJamaah.Infrastructure.Excel;

public class JamaahExcel : IJamaahExcel
{
    public const string SheetData = "Data Jamaah";
    public const string SheetPetunjuk = "Petunjuk";

    private static readonly string[] Headers =
        ["Nama*", "Nomor HP*", "Alamat*", "Tanggal Lahir*", "Jenis Kelamin*"];

    private static readonly string[] DateFormats =
        ["dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd"];

    private const int TemplateRows = JamaahImportService.MaxBaris;

    public byte[] BuatTemplate()
    {
        using var wb = new XLWorkbook();

        var data = wb.Worksheets.Add(SheetData);
        for (var i = 0; i < Headers.Length; i++)
        {
            var cell = data.Cell(1, i + 1);
            cell.Value = Headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCE9F7");
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        }

        data.Column(1).Width = 30;
        data.Column(2).Width = 18;
        data.Column(3).Width = 45;
        data.Column(4).Width = 16;
        data.Column(5).Width = 16;

        // Nomor HP sebagai teks agar angka 0 di depan tidak hilang; tanggal berformat dd/mm/yyyy.
        data.Column(2).Style.NumberFormat.Format = "@";
        data.Column(4).Style.DateFormat.Format = "dd/mm/yyyy";

        var jenisKelamin = data.Range(2, 5, TemplateRows + 1, 5).CreateDataValidation();
        jenisKelamin.List("\"Laki-laki,Perempuan\"");
        jenisKelamin.ErrorTitle = "Jenis kelamin";
        jenisKelamin.ErrorMessage = "Pilih Laki-laki atau Perempuan.";

        data.SheetView.FreezeRows(1);

        var petunjuk = wb.Worksheets.Add(SheetPetunjuk);
        petunjuk.Column(1).Width = 28;
        petunjuk.Column(2).Width = 18;
        petunjuk.Column(3).Width = 36;
        petunjuk.Column(4).Width = 16;
        petunjuk.Column(5).Width = 16;

        var lines = new[]
        {
            "Petunjuk pengisian",
            $"1. Isi data pada sheet \"{SheetData}\" mulai baris 2. Jangan mengubah atau menghapus baris header (baris 1).",
            "2. Semua kolom bertanda * wajib diisi.",
            "3. Nomor HP: awali dengan 08, 628, atau +628 (contoh: 081234567890).",
            "4. Tanggal Lahir: format dd/MM/yyyy (contoh: 17/05/1990).",
            "5. Jenis Kelamin: Laki-laki atau Perempuan.",
            "6. Nomor Jamaah dibuat otomatis oleh sistem, jangan diisi.",
            $"7. Maksimal {JamaahImportService.MaxBaris} baris per file.",
            "8. Bila ada satu baris yang salah, tidak ada data yang diimpor; perbaiki lalu unggah ulang.",
            "9. Jamaah dengan nama dan nomor HP yang sama dengan data yang sudah ada akan dilewati."
        };
        for (var i = 0; i < lines.Length; i++)
            petunjuk.Cell(i + 1, 1).Value = lines[i];
        petunjuk.Cell(1, 1).Style.Font.Bold = true;

        const int contohHeaderRow = 12;
        petunjuk.Cell(contohHeaderRow - 1, 1).Value = "Contoh pengisian (jangan disalin sebagai data):";
        petunjuk.Cell(contohHeaderRow - 1, 1).Style.Font.Bold = true;
        for (var i = 0; i < Headers.Length; i++)
        {
            var cell = petunjuk.Cell(contohHeaderRow, i + 1);
            cell.Value = Headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEEEEE");
        }

        petunjuk.Range(contohHeaderRow + 1, 2, contohHeaderRow + 2, 2).Style.NumberFormat.Format = "@";
        petunjuk.Cell(contohHeaderRow + 1, 1).Value = "Ahmad Fauzi";
        petunjuk.Cell(contohHeaderRow + 1, 2).Value = "081234567890";
        petunjuk.Cell(contohHeaderRow + 1, 3).Value = "Jl. Mawar No. 1, Bandung";
        petunjuk.Cell(contohHeaderRow + 1, 4).Value = new DateTime(1990, 5, 17);
        petunjuk.Cell(contohHeaderRow + 1, 5).Value = "Laki-laki";
        petunjuk.Cell(contohHeaderRow + 2, 1).Value = "Siti Aminah";
        petunjuk.Cell(contohHeaderRow + 2, 2).Value = "+6285712345678";
        petunjuk.Cell(contohHeaderRow + 2, 3).Value = "Jl. Melati No. 7, Bandung";
        petunjuk.Cell(contohHeaderRow + 2, 4).Value = new DateTime(1985, 11, 2);
        petunjuk.Cell(contohHeaderRow + 2, 5).Value = "Perempuan";
        petunjuk.Range(contohHeaderRow + 1, 4, contohHeaderRow + 2, 4).Style.DateFormat.Format = "dd/mm/yyyy";

        data.SetTabActive();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public IReadOnlyList<JamaahImportRow> Baca(Stream stream, int maxBaris)
    {
        XLWorkbook wb;
        try
        {
            wb = new XLWorkbook(stream);
        }
        catch (Exception)
        {
            throw new ImportFileException("File tidak dapat dibaca. Pastikan formatnya .xlsx (Excel) dan tidak rusak.");
        }

        using (wb)
        {
            var ws = wb.Worksheets.FirstOrDefault(w => w.Name == SheetData) ?? wb.Worksheets.FirstOrDefault()
                ?? throw new ImportFileException("File tidak memiliki sheet.");

            for (var c = 0; c < Headers.Length; c++)
            {
                if (NormalizeHeader(ws.Cell(1, c + 1).GetString()) != NormalizeHeader(Headers[c]))
                    throw new ImportFileException(
                        $"Header kolom {c + 1} harus \"{Headers[c].TrimEnd('*')}\". Gunakan template yang disediakan dan jangan ubah baris header.");
            }

            var rows = new List<JamaahImportRow>();
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

            for (var r = 2; r <= lastRow; r++)
            {
                var nama = ws.Cell(r, 1).GetString();
                var hp = ReadNomorHp(ws.Cell(r, 2));
                var alamat = ws.Cell(r, 3).GetString();
                var (tanggal, tanggalTeks) = ReadTanggal(ws.Cell(r, 4));
                var jk = ws.Cell(r, 5).GetString();

                if (string.IsNullOrWhiteSpace(nama) && string.IsNullOrWhiteSpace(hp) &&
                    string.IsNullOrWhiteSpace(alamat) && tanggal is null && string.IsNullOrWhiteSpace(tanggalTeks) &&
                    string.IsNullOrWhiteSpace(jk))
                {
                    continue;
                }

                rows.Add(new JamaahImportRow(r, nama, hp, alamat, tanggal, tanggalTeks, jk));
                if (rows.Count > maxBaris) break;
            }

            return rows;
        }
    }

    private static string NormalizeHeader(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    /// <summary>Excel sering menyimpan HP sebagai angka sehingga 0 di depan hilang (81234... → 081234...).</summary>
    private static string ReadNomorHp(IXLCell cell)
    {
        if (cell.DataType == XLDataType.Number)
        {
            var text = cell.GetDouble().ToString("F0", CultureInfo.InvariantCulture);
            return text.StartsWith('8') ? "0" + text : text;
        }

        return cell.GetString().Trim();
    }

    private static (DateOnly? Tanggal, string? Teks) ReadTanggal(IXLCell cell)
    {
        if (cell.IsEmpty()) return (null, null);

        if (cell.DataType == XLDataType.DateTime)
            return (DateOnly.FromDateTime(cell.GetDateTime()), null);

        var text = cell.GetString().Trim();
        if (text.Length == 0) return (null, null);

        return DateTime.TryParseExact(text, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? (DateOnly.FromDateTime(parsed), text)
            : (null, text);
    }
}
