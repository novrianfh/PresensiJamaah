namespace PresensiJamaah.Application.Models;

public enum RekapPeriode
{
    Minggu,
    Bulan,
    Tahun
}

/// <param name="Periode">Jenis periode.</param>
/// <param name="Tanggal">Tanggal acuan; periode adalah minggu (Senin–Minggu), bulan, atau tahun yang memuat tanggal ini.</param>
/// <param name="EventId">Bila diisi, hanya event ini yang dihitung.</param>
public record RekapQuery(RekapPeriode Periode, DateOnly Tanggal, Guid? EventId = null);

/// <param name="Hadir">Jumlah kehadiran (kejadian yang dihadiri) pada periode.</param>
/// <param name="Persen">Hadir / total kejadian × 100, dibatasi 0–100.</param>
/// <param name="Aktif">True bila Persen mencapai batas minimal jamaah aktif.</param>
public record RekapRow(Guid JamaahId, string NomorJamaah, string Nama, int Hadir, decimal Persen, bool Aktif);

/// <param name="Dari">Awal periode (inklusif).</param>
/// <param name="Sampai">Akhir periode (inklusif), bisa di masa depan untuk periode berjalan.</param>
/// <param name="EventNama">Nama event bila difilter, selain itu null (semua event).</param>
/// <param name="TotalKejadian">Jumlah kejadian yang sudah dimulai pada periode (penyebut persentase).</param>
/// <param name="MinimalPersen">Batas minimal persentase untuk jamaah aktif.</param>
public record RekapResult(
    RekapPeriode Periode,
    DateOnly Dari,
    DateOnly Sampai,
    Guid? EventId,
    string? EventNama,
    int TotalKejadian,
    decimal MinimalPersen,
    IReadOnlyList<RekapRow> Rows)
{
    public int JumlahAktif => Rows.Count(r => r.Aktif);
}
