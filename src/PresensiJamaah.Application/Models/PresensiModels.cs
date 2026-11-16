namespace PresensiJamaah.Application.Models;

public enum ScanStatus
{
    /// <summary>Presensi baru tercatat pada minimal satu event.</summary>
    Berhasil,
    /// <summary>Jamaah sudah tercatat hadir pada semua event yang sedang berlangsung; tidak ada data baru.</summary>
    SudahTercatat,
    NomorKosong,
    JamaahTidakDitemukan,
    JamaahNonaktif,
    /// <summary>Saat scan tidak ada event yang sedang berlangsung.</summary>
    EventTidakBerlangsung
}

/// <param name="Status">Hasil scan.</param>
/// <param name="Pesan">Pesan siap tampil untuk pengurus/jamaah.</param>
/// <param name="NamaJamaah">Terisi bila jamaah ditemukan (termasuk saat nonaktif).</param>
/// <param name="NomorJamaah">Nomor jamaah yang dipindai (sudah dinormalkan).</param>
/// <param name="NamaEvent">Event yang dikenai scan: yang baru tercatat (Berhasil) atau yang sudah tercatat (SudahTercatat).</param>
/// <param name="WaktuScan">Hanya terisi untuk Berhasil.</param>
public record ScanResult(
    ScanStatus Status,
    string Pesan,
    string? NamaJamaah = null,
    string? NomorJamaah = null,
    IReadOnlyList<string>? NamaEvent = null,
    DateTime? WaktuScan = null)
{
    public bool IsBerhasil => Status == ScanStatus.Berhasil;
}
