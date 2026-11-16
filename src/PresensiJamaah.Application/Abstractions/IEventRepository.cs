using PresensiJamaah.Domain.Entities;

namespace PresensiJamaah.Application.Abstractions;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Semua event, urut menurut nama.</summary>
    Task<IReadOnlyList<Event>> ListAsync(CancellationToken ct = default);

    /// <summary>Event yang mungkin punya kejadian pada [from, to] (inklusif). Hasil boleh lebih longgar dari kenyataan.</summary>
    Task<IReadOnlyList<Event>> ListCandidatesAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>True bila event sudah punya catatan presensi.</summary>
    Task<bool> HasPresensiAsync(Guid eventId, CancellationToken ct = default);

    Task AddAsync(Event e, CancellationToken ct = default);

    void Remove(Event e);

    Task SaveChangesAsync(CancellationToken ct = default);
}
