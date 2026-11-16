using PresensiJamaah.Application.Models;

namespace PresensiJamaah.Application.Abstractions;

/// <summary>Membuat template dan membaca file Excel (.xlsx) untuk impor jamaah.</summary>
public interface IJamaahExcel
{
    /// <summary>Menghasilkan file template .xlsx (header, petunjuk, dan contoh pengisian).</summary>
    byte[] BuatTemplate();

    /// <summary>
    /// Membaca baris data dari file. Baris kosong dilewati. Pembacaan berhenti setelah
    /// <paramref name="maxBaris"/> baris terisi (agar file raksasa tidak dibaca seluruhnya).
    /// </summary>
    /// <exception cref="ImportFileException">File bukan .xlsx yang valid atau header tidak sesuai template.</exception>
    IReadOnlyList<JamaahImportRow> Baca(Stream stream, int maxBaris);
}
