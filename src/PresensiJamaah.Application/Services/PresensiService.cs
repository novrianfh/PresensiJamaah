using PresensiJamaah.Application.Abstractions;
using PresensiJamaah.Application.Common;
using PresensiJamaah.Application.Models;
using PresensiJamaah.Domain.Entities;

namespace PresensiJamaah.Application.Services;

public class PresensiService(
    IJamaahRepository jamaahRepository,
    IEventRepository eventRepository,
    IPresensiRepository presensiRepository,
    LocalClock clock)
{
    /// <summary>
    /// Kejadian yang sedang berlangsung sekarang (Mulai &lt;= sekarang &lt;= Selesai), menurut waktu lokal aplikasi.
    /// Kemarin ikut dihitung karena event bisa melewati tengah malam.
    /// </summary>
    public async Task<IReadOnlyList<EventOccurrence>> ListBerlangsungAsync(CancellationToken ct = default) =>
        await ListBerlangsungAsync(clock.Now(), ct);

    private async Task<IReadOnlyList<EventOccurrence>> ListBerlangsungAsync(DateTime now, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(now);
        var from = today.AddDays(-1);

        var candidates = await eventRepository.ListCandidatesAsync(from, today, ct);
        return candidates
            .SelectMany(e => e.GetOccurrences(from, today))
            .Where(o => IsBerlangsung(o, now))
            .OrderBy(o => o.Mulai).ThenBy(o => o.Nama)
            .ToList();
    }

    /// <summary>
    /// Mencatat kehadiran jamaah (isi QR / nomor jamaah) pada event yang sedang berlangsung saat scan.
    /// Bila beberapa event berlangsung bersamaan, kehadiran dicatat pada semuanya.
    /// </summary>
    public async Task<ScanResult> ScanAsync(string? nomorJamaah, CancellationToken ct = default)
    {
        var nomor = nomorJamaah?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(nomor))
            return new ScanResult(ScanStatus.NomorKosong, "Nomor jamaah kosong.");

        var now = clock.Now();
        var berlangsung = await ListBerlangsungAsync(now, ct);
        if (berlangsung.Count == 0)
            return new ScanResult(
                ScanStatus.EventTidakBerlangsung, "Saat ini tidak ada event yang sedang berlangsung.", NomorJamaah: nomor);

        var jamaah = await jamaahRepository.GetByNomorJamaahAsync(nomor, ct);
        if (jamaah is null)
            return new ScanResult(
                ScanStatus.JamaahTidakDitemukan, $"Jamaah dengan nomor {nomor} tidak ditemukan.", NomorJamaah: nomor);

        if (!jamaah.IsActive)
            return new ScanResult(
                ScanStatus.JamaahNonaktif, $"{jamaah.Nama} berstatus nonaktif.", jamaah.Nama, jamaah.NomorJamaah);

        var baru = new List<string>();
        var sudah = new List<string>();
        foreach (var kejadian in berlangsung)
        {
            var tercatat =
                await presensiRepository.ExistsAsync(jamaah.Id, kejadian.EventId, kejadian.Tanggal, ct) ||
                !await presensiRepository.TryAddAsync(
                    Presensi.Create(jamaah.Id, kejadian.EventId, kejadian.Tanggal, now), ct); // false = scan lain menyimpan lebih dulu

            (tercatat ? sudah : baru).Add(kejadian.Nama);
        }

        if (baru.Count == 0)
            return new ScanResult(
                ScanStatus.SudahTercatat,
                $"{jamaah.Nama} sudah tercatat hadir pada {Gabung(sudah)}.",
                jamaah.Nama, jamaah.NomorJamaah, sudah);

        return new ScanResult(
            ScanStatus.Berhasil,
            $"Terima kasih sudah melakukan scan. Kehadiran Anda sudah tercatat pada {Gabung(baru)}.",
            jamaah.Nama, jamaah.NomorJamaah, baru, now);
    }

    private static string Gabung(IReadOnlyList<string> nama) => nama.Count switch
    {
        1 => nama[0],
        2 => $"{nama[0]} dan {nama[1]}",
        _ => $"{string.Join(", ", nama.Take(nama.Count - 1))}, dan {nama[^1]}"
    };

    private static bool IsBerlangsung(EventOccurrence o, DateTime now) => o.Mulai <= now && now <= o.Selesai;
}
