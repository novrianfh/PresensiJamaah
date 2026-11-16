using PresensiJamaah.Application.Abstractions;
using PresensiJamaah.Domain.Entities;

namespace PresensiJamaah.Application.Tests;

internal sealed class FakeEventRepository : IEventRepository
{
    public List<Event> Data { get; } = [];
    public int SaveCount { get; private set; }

    public Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Data.FirstOrDefault(e => e.Id == id));

    public Task<IReadOnlyList<Event>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Event>>(Data.OrderBy(e => e.Nama).ToList());

    // Longgar: semua event dikembalikan; service/entitas yang menyaring kejadian sebenarnya.
    public Task<IReadOnlyList<Event>> ListCandidatesAsync(DateOnly from, DateOnly to, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Event>>(Data.ToList());

    /// <summary>Id event yang dianggap sudah punya presensi.</summary>
    public HashSet<Guid> EventDenganPresensi { get; } = [];

    public Task<bool> HasPresensiAsync(Guid eventId, CancellationToken ct = default) =>
        Task.FromResult(EventDenganPresensi.Contains(eventId));

    public Task AddAsync(Event e, CancellationToken ct = default)
    {
        Data.Add(e);
        return Task.CompletedTask;
    }

    public void Remove(Event e) => Data.Remove(e);

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
