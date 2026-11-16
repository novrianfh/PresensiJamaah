using PresensiJamaah.Application.Common;
using PresensiJamaah.Application.Models;
using PresensiJamaah.Domain.Entities;

namespace PresensiJamaah.Application.Abstractions;

public interface IJamaahRepository
{
    Task<Jamaah?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Mencari berdasarkan NomorJamaah persis (isi QR). Null bila tidak ada.</summary>
    Task<Jamaah?> GetByNomorJamaahAsync(string nomorJamaah, CancellationToken ct = default);

    Task<PagedResult<Jamaah>> ListAsync(JamaahQuery query, CancellationToken ct = default);

    /// <summary>Nama dan nomor HP semua jamaah (aktif maupun nonaktif), untuk deteksi duplikat saat impor.</summary>
    Task<IReadOnlyList<(string Nama, string NomorHp)>> ListNamaNomorHpAsync(CancellationToken ct = default);

    /// <summary>Mengambil nomor urut berikutnya yang dijamin unik (sequence database).</summary>
    Task<long> NextNomorUrutAsync(CancellationToken ct = default);

    Task AddAsync(Jamaah jamaah, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
