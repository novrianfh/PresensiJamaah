namespace PresensiJamaah.Domain.Entities;

/// <summary>
/// Catatan kehadiran satu jamaah pada satu kejadian event.
/// Unik per (JamaahId, EventId, TanggalKejadian). Tidak punya navigasi ke Identity.
/// </summary>
public class Presensi
{
    private Presensi() { } // untuk EF Core

    public Guid Id { get; private set; }
    public Guid JamaahId { get; private set; }
    public Guid EventId { get; private set; }

    /// <summary>Tanggal mulai kejadian event (bukan tanggal scan, bisa berbeda untuk event lewat tengah malam).</summary>
    public DateOnly TanggalKejadian { get; private set; }

    /// <summary>Waktu scan, waktu lokal tanpa zona.</summary>
    public DateTime WaktuScan { get; private set; }

    public static Presensi Create(Guid jamaahId, Guid eventId, DateOnly tanggalKejadian, DateTime waktuScan)
    {
        if (jamaahId == Guid.Empty) throw new ArgumentException("Jamaah wajib diisi.", nameof(jamaahId));
        if (eventId == Guid.Empty) throw new ArgumentException("Event wajib diisi.", nameof(eventId));

        return new Presensi
        {
            Id = Guid.NewGuid(),
            JamaahId = jamaahId,
            EventId = eventId,
            TanggalKejadian = tanggalKejadian,
            WaktuScan = DateTime.SpecifyKind(waktuScan, DateTimeKind.Unspecified)
        };
    }
}
