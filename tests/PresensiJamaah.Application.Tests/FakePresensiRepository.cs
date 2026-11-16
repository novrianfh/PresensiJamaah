using PresensiJamaah.Application.Abstractions;
using PresensiJamaah.Domain.Entities;

namespace PresensiJamaah.Application.Tests;

internal sealed class FakePresensiRepository : IPresensiRepository
{
    public List<Presensi> Data { get; } = [];

    /// <summary>Bila true, TryAddAsync berperilaku seolah scan lain menyimpan lebih dulu (balapan).</summary>
    public bool SimulasiBalapan { get; set; }

    public Task<bool> ExistsAsync(Guid jamaahId, Guid eventId, DateOnly tanggalKejadian, CancellationToken ct = default) =>
        Task.FromResult(Data.Any(p => p.JamaahId == jamaahId && p.EventId == eventId && p.TanggalKejadian == tanggalKejadian));

    public Task<IReadOnlyDictionary<Guid, int>> CountPerJamaahAsync(
        DateOnly from, DateOnly to, Guid? eventId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, int>>(
            Data.Where(p => p.TanggalKejadian >= from && p.TanggalKejadian <= to && (eventId is null || p.EventId == eventId))
                .GroupBy(p => p.JamaahId)
                .ToDictionary(g => g.Key, g => g.Count()));

    public Task<bool> TryAddAsync(Presensi presensi, CancellationToken ct = default)
    {
        if (SimulasiBalapan ||
            Data.Any(p => p.JamaahId == presensi.JamaahId && p.EventId == presensi.EventId &&
                          p.TanggalKejadian == presensi.TanggalKejadian))
            return Task.FromResult(false);

        Data.Add(presensi);
        return Task.FromResult(true);
    }
}
