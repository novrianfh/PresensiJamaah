using PresensiJamaah.Domain.Enums;

namespace PresensiJamaah.Domain.Entities;

/// <summary>
/// Kajian atau sholat berjamaah. Event berulang disimpan sebagai satu aturan (jam mulai/selesai dari
/// WaktuMulai/WaktuSelesai); kejadiannya dihitung lewat <see cref="GetOccurrences"/>.
/// Waktu adalah waktu lokal tanpa zona.
/// </summary>
public class Event
{
    public const int NamaMaxLength = 150;
    public const int DeskripsiMaxLength = 1000;

    private Event() { } // untuk EF Core

    public Guid Id { get; private set; }
    public string Nama { get; private set; } = string.Empty;
    public string Deskripsi { get; private set; } = string.Empty;
    public DateTime WaktuMulai { get; private set; }
    public DateTime WaktuSelesai { get; private set; }
    public Recurrence Recurrence { get; private set; }

    /// <summary>Tanggal terakhir event berulang boleh terjadi (inklusif). Null = tanpa batas. Selalu null bila tidak berulang.</summary>
    public DateOnly? RecurrenceEndDate { get; private set; }

    public DateOnly TanggalMulai => DateOnly.FromDateTime(WaktuMulai);

    public static Event Create(
        string nama, string? deskripsi, DateTime waktuMulai, DateTime waktuSelesai,
        Recurrence recurrence, DateOnly? recurrenceEndDate)
    {
        var e = new Event { Id = Guid.NewGuid() };
        e.Update(nama, deskripsi, waktuMulai, waktuSelesai, recurrence, recurrenceEndDate);
        return e;
    }

    public void Update(
        string nama, string? deskripsi, DateTime waktuMulai, DateTime waktuSelesai,
        Recurrence recurrence, DateOnly? recurrenceEndDate)
    {
        if (!Enum.IsDefined(recurrence))
            throw new ArgumentException("Pengulangan tidak valid.", nameof(recurrence));

        var trimmedNama = nama?.Trim();
        if (string.IsNullOrEmpty(trimmedNama))
            throw new ArgumentException("Nama wajib diisi.", nameof(nama));
        if (trimmedNama.Length > NamaMaxLength)
            throw new ArgumentException($"Nama maksimal {NamaMaxLength} karakter.", nameof(nama));

        var trimmedDeskripsi = deskripsi?.Trim() ?? string.Empty;
        if (trimmedDeskripsi.Length > DeskripsiMaxLength)
            throw new ArgumentException($"Deskripsi maksimal {DeskripsiMaxLength} karakter.", nameof(deskripsi));

        var mulai = DateTime.SpecifyKind(waktuMulai, DateTimeKind.Unspecified);
        var selesai = DateTime.SpecifyKind(waktuSelesai, DateTimeKind.Unspecified);

        if (selesai <= mulai)
            throw new ArgumentException("Waktu selesai harus setelah waktu mulai.", nameof(waktuSelesai));

        DateOnly? endDate = null;
        if (recurrence != Recurrence.None)
        {
            if (selesai - mulai > TimeSpan.FromHours(24))
                throw new ArgumentException("Event berulang maksimal berdurasi 24 jam.", nameof(waktuSelesai));

            if (recurrenceEndDate is { } end)
            {
                if (end < DateOnly.FromDateTime(mulai))
                    throw new ArgumentException(
                        "Batas pengulangan tidak boleh sebelum tanggal mulai.", nameof(recurrenceEndDate));
                endDate = end;
            }
        }

        Nama = trimmedNama;
        Deskripsi = trimmedDeskripsi;
        WaktuMulai = mulai;
        WaktuSelesai = selesai;
        Recurrence = recurrence;
        RecurrenceEndDate = endDate;
    }

    /// <summary>Kejadian event pada rentang tanggal [from, to] (inklusif), berurutan menurut tanggal.</summary>
    public IReadOnlyList<EventOccurrence> GetOccurrences(DateOnly from, DateOnly to)
    {
        if (to < from) return [];

        var first = TanggalMulai;

        if (Recurrence == Recurrence.None)
            return first >= from && first <= to ? [Build(first)] : [];

        var step = Recurrence == Recurrence.Harian ? 1 : 7;
        var last = RecurrenceEndDate is { } end && end < to ? end : to;

        var current = first;
        if (from > first)
        {
            var gap = from.DayNumber - first.DayNumber;
            current = first.AddDays((gap + step - 1) / step * step);
        }

        var result = new List<EventOccurrence>();
        for (; current <= last; current = current.AddDays(step))
            result.Add(Build(current));
        return result;
    }

    private EventOccurrence Build(DateOnly date)
    {
        var mulai = date.ToDateTime(TimeOnly.FromDateTime(WaktuMulai), DateTimeKind.Unspecified);
        return new EventOccurrence(Id, Nama, date, mulai, mulai + (WaktuSelesai - WaktuMulai));
    }
}
