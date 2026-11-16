using PresensiJamaah.Application.Abstractions;
using PresensiJamaah.Application.Models;
using PresensiJamaah.Domain.Entities;

namespace PresensiJamaah.Application.Services;

public class EventService(IEventRepository repository)
{
    /// <summary>Rentang maksimal (hari) untuk perhitungan kejadian, agar satu permintaan tetap ringan.</summary>
    public const int MaxRangeDays = 366;

    public async Task<IReadOnlyList<EventDto>> ListAsync(CancellationToken ct = default) =>
        (await repository.ListAsync(ct)).Select(ToDto).ToList();

    public async Task<EventDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var e = await repository.GetByIdAsync(id, ct);
        return e is null ? null : ToDto(e);
    }

    public async Task<EventDto> CreateAsync(EventInput input, CancellationToken ct = default)
    {
        var e = Event.Create(
            input.Nama, input.Deskripsi, input.WaktuMulai, input.WaktuSelesai, input.Recurrence, input.RecurrenceEndDate);
        await repository.AddAsync(e, ct);
        await repository.SaveChangesAsync(ct);
        return ToDto(e);
    }

    /// <returns>Data terbaru, atau null bila event tidak ditemukan.</returns>
    public async Task<EventDto?> UpdateAsync(Guid id, EventInput input, CancellationToken ct = default)
    {
        var e = await repository.GetByIdAsync(id, ct);
        if (e is null) return null;

        e.Update(input.Nama, input.Deskripsi, input.WaktuMulai, input.WaktuSelesai, input.Recurrence, input.RecurrenceEndDate);
        await repository.SaveChangesAsync(ct);
        return ToDto(e);
    }

    /// <returns>False bila event tidak ditemukan.</returns>
    /// <exception cref="InvalidOperationException">Event sudah punya presensi sehingga tidak boleh dihapus.</exception>
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var e = await repository.GetByIdAsync(id, ct);
        if (e is null) return false;

        if (await repository.HasPresensiAsync(id, ct))
            throw new InvalidOperationException("Event sudah memiliki data presensi sehingga tidak dapat dihapus.");

        repository.Remove(e);
        await repository.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>Kejadian semua event pada [from, to] (inklusif), urut menurut waktu mulai lalu nama.</summary>
    public async Task<IReadOnlyList<EventOccurrence>> GetOccurrencesAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        ValidateRange(from, to);

        var candidates = await repository.ListCandidatesAsync(from, to, ct);
        return candidates
            .SelectMany(e => e.GetOccurrences(from, to))
            .OrderBy(o => o.Mulai).ThenBy(o => o.Nama)
            .ToList();
    }

    /// <summary>Kejadian satu event pada [from, to]; null bila event tidak ditemukan.</summary>
    public async Task<IReadOnlyList<EventOccurrence>?> GetOccurrencesAsync(
        Guid id, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        ValidateRange(from, to);

        var e = await repository.GetByIdAsync(id, ct);
        return e?.GetOccurrences(from, to);
    }

    private static void ValidateRange(DateOnly from, DateOnly to)
    {
        if (to < from)
            throw new ArgumentException("Tanggal akhir tidak boleh sebelum tanggal awal.", nameof(to));
        if (to.DayNumber - from.DayNumber >= MaxRangeDays)
            throw new ArgumentOutOfRangeException(nameof(to), $"Rentang maksimal {MaxRangeDays} hari.");
    }

    private static EventDto ToDto(Event e) => new(
        e.Id, e.Nama, e.Deskripsi, e.WaktuMulai, e.WaktuSelesai, e.Recurrence, e.RecurrenceEndDate);
}
