namespace PresensiJamaah.Application.Models;

/// <summary>Satu baris mentah dari file Excel. BarisKe = nomor baris di Excel (header = baris 1).</summary>
public record JamaahImportRow(
    int BarisKe,
    string? Nama,
    string? NomorHp,
    string? Alamat,
    DateOnly? TanggalLahir,
    string? TanggalLahirTeks,
    string? JenisKelaminTeks);

public record ImportRowMessage(int BarisKe, string Pesan);

public record JamaahImportResult(
    int Diimpor,
    IReadOnlyList<ImportRowMessage> Dilewati,
    IReadOnlyList<ImportRowMessage> Kesalahan)
{
    /// <summary>Impor dianggap gagal total bila ada satu saja baris yang salah (tidak ada data tersimpan).</summary>
    public bool Berhasil => Kesalahan.Count == 0;
}

/// <summary>File tidak bisa diproses (bukan .xlsx yang valid atau header tidak sesuai template).</summary>
public class ImportFileException(string message) : Exception(message);
