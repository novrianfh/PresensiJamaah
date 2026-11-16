using PresensiJamaah.Domain.Entities;

namespace PresensiJamaah.Application.Abstractions;

public interface IPresensiRepository
{
    Task<bool> ExistsAsync(Guid jamaahId, Guid eventId, DateOnly tanggalKejadian, CancellationToken ct = default);

    /// <summary>
    /// Jumlah presensi per jamaah dengan TanggalKejadian pada [from, to] (inklusif).
    /// Jamaah tanpa presensi tidak ada di hasil. <paramref name="eventId"/> null = semua event.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, int>> CountPerJamaahAsync(
        DateOnly from, DateOnly to, Guid? eventId, CancellationToken ct = default);

    /// <summary>
    /// Menyimpan presensi. Mengembalikan false (tanpa menyimpan apa pun) bila kombinasi
    /// (jamaah, event, tanggal) sudah ada, termasuk saat dua scan bersamaan berebut.
    /// </summary>
    Task<bool> TryAddAsync(Presensi presensi, CancellationToken ct = default);
}
