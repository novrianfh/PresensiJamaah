using PresensiJamaah.Application.Abstractions;
using PresensiJamaah.Application.Common;
using PresensiJamaah.Application.Models;
using PresensiJamaah.Domain.Entities;

namespace PresensiJamaah.Application.Services;

public class RekapService(
    JamaahService jamaahService,
    IEventRepository eventRepository,
    IPresensiRepository presensiRepository,
    LocalClock clock,
    RekapOptions options)
{
    /// <summary>Rentang tanggal periode yang memuat <paramref name="tanggal"/>. Minggu = Senin–Minggu.</summary>
    public static (DateOnly Dari, DateOnly Sampai) HitungRentang(RekapPeriode periode, DateOnly tanggal)
    {
        switch (periode)
        {
            case RekapPeriode.Minggu:
                var sejakSenin = ((int)tanggal.DayOfWeek + 6) % 7; // Senin=0 … Minggu=6
                var senin = tanggal.AddDays(-sejakSenin);
                return (senin, senin.AddDays(6));
            case RekapPeriode.Bulan:
                var awalBulan = new DateOnly(tanggal.Year, tanggal.Month, 1);
                return (awalBulan, awalBulan.AddMonths(1).AddDays(-1));
            case RekapPeriode.Tahun:
                return (new DateOnly(tanggal.Year, 1, 1), new DateOnly(tanggal.Year, 12, 31));
            default:
                throw new ArgumentOutOfRangeException(nameof(periode), "Periode tidak valid.");
        }
    }

    /// <summary>Persentase kehadiran (0–100). Tanpa kejadian, hasilnya 0. Dibatasi 100 bila data presensi melebihi jumlah kejadian.</summary>
    public static decimal HitungPersen(int hadir, int totalKejadian)
    {
        if (hadir < 0) throw new ArgumentOutOfRangeException(nameof(hadir));
        if (totalKejadian <= 0) return 0m;
        return Math.Min(100m, hadir * 100m / totalKejadian);
    }

    /// <summary>Aktif bila ada kejadian dan hadir/total × 100 >= <paramref name="minimalPersen"/> (dibandingkan tanpa pembulatan).</summary>
    public static bool IsAktif(int hadir, int totalKejadian, decimal minimalPersen) =>
        totalKejadian > 0 && Math.Min(hadir, totalKejadian) * 100m >= minimalPersen * totalKejadian;

    /// <summary>
    /// Rekap jamaah aktif (terdaftar) pada periode. Penyebut = kejadian yang sudah dimulai pada periode
    /// (kejadian yang belum terjadi tidak dihitung). Null bila event filter tidak ditemukan.
    /// </summary>
    public async Task<RekapResult?> RekapAsync(RekapQuery query, CancellationToken ct = default)
    {
        var (dari, sampai) = HitungRentang(query.Periode, query.Tanggal);
        var now = clock.Now();

        string? eventNama = null;
        IReadOnlyList<Event> events;
        if (query.EventId is { } eventId)
        {
            var e = await eventRepository.GetByIdAsync(eventId, ct);
            if (e is null) return null;
            eventNama = e.Nama;
            events = [e];
        }
        else
        {
            events = await eventRepository.ListCandidatesAsync(dari, sampai, ct);
        }

        var totalKejadian = events
            .SelectMany(e => e.GetOccurrences(dari, sampai))
            .Count(o => o.Mulai <= now);

        var jamaah = await jamaahService.ListAllAktifAsync(ct);
        var hadirPerJamaah = await presensiRepository.CountPerJamaahAsync(dari, sampai, query.EventId, ct);

        var rows = jamaah
            .Select(j =>
            {
                var hadir = hadirPerJamaah.GetValueOrDefault(j.Id);
                return new RekapRow(
                    j.Id, j.NomorJamaah, j.Nama, hadir,
                    HitungPersen(hadir, totalKejadian),
                    IsAktif(hadir, totalKejadian, options.JamaahAktifMinimalPersen));
            })
            .OrderByDescending(r => r.Persen).ThenBy(r => r.Nama).ThenBy(r => r.NomorJamaah)
            .ToList();

        return new RekapResult(
            query.Periode, dari, sampai, query.EventId, eventNama, totalKejadian, options.JamaahAktifMinimalPersen, rows);
    }
}
