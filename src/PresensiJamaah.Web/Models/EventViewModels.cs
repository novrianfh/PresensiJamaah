using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using PresensiJamaah.Application.Models;
using PresensiJamaah.Domain.Entities;
using PresensiJamaah.Domain.Enums;

namespace PresensiJamaah.Web.Models;

public static class RecurrenceExtensions
{
    public static string ToLabel(this Recurrence recurrence) => recurrence switch
    {
        Recurrence.None => "Sekali",
        Recurrence.Harian => "Setiap hari",
        Recurrence.Mingguan => "Setiap minggu",
        _ => "-"
    };

    public static IEnumerable<SelectListItem> ToSelectList(Recurrence selected) =>
        Enum.GetValues<Recurrence>()
            .Select(v => new SelectListItem(v.ToLabel(), v.ToString(), v == selected));
}

public class EventFormViewModel : IValidatableObject
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Nama wajib diisi")]
    [StringLength(Event.NamaMaxLength, ErrorMessage = "Nama maksimal {1} karakter")]
    public string Nama { get; set; } = string.Empty;

    [StringLength(Event.DeskripsiMaxLength, ErrorMessage = "Deskripsi maksimal {1} karakter")]
    public string? Deskripsi { get; set; }

    [Display(Name = "Waktu Mulai")]
    [Required(ErrorMessage = "Waktu mulai wajib diisi")]
    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? WaktuMulai { get; set; }

    [Display(Name = "Waktu Selesai")]
    [Required(ErrorMessage = "Waktu selesai wajib diisi")]
    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
    public DateTime? WaktuSelesai { get; set; }

    [Display(Name = "Pengulangan")]
    public Recurrence Recurrence { get; set; } = Recurrence.None;

    [Display(Name = "Berulang sampai (opsional)")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateOnly? RecurrenceEndDate { get; set; }

    public IEnumerable<SelectListItem> RecurrenceOptions => RecurrenceExtensions.ToSelectList(Recurrence);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Enum.IsDefined(Recurrence))
            yield return new ValidationResult("Pengulangan tidak valid", [nameof(Recurrence)]);

        if (WaktuMulai is not { } mulai || WaktuSelesai is not { } selesai) yield break;

        if (selesai <= mulai)
            yield return new ValidationResult("Waktu selesai harus setelah waktu mulai", [nameof(WaktuSelesai)]);

        if (Recurrence != Recurrence.None)
        {
            if (selesai - mulai > TimeSpan.FromHours(24))
                yield return new ValidationResult(
                    "Event berulang maksimal berdurasi 24 jam", [nameof(WaktuSelesai)]);

            if (RecurrenceEndDate is { } end && end < DateOnly.FromDateTime(mulai))
                yield return new ValidationResult(
                    "Batas pengulangan tidak boleh sebelum tanggal mulai", [nameof(RecurrenceEndDate)]);
        }
    }

    /// <summary>Hanya dipanggil setelah ModelState valid.</summary>
    public EventInput ToInput() => new(
        Nama.Trim(),
        Deskripsi?.Trim(),
        WaktuMulai!.Value,
        WaktuSelesai!.Value,
        Recurrence,
        Recurrence == Recurrence.None ? null : RecurrenceEndDate);

    public static EventFormViewModel From(EventDto dto) => new()
    {
        Id = dto.Id,
        Nama = dto.Nama,
        Deskripsi = dto.Deskripsi,
        WaktuMulai = dto.WaktuMulai,
        WaktuSelesai = dto.WaktuSelesai,
        Recurrence = dto.Recurrence,
        RecurrenceEndDate = dto.RecurrenceEndDate
    };
}

public class EventDetailsViewModel
{
    public required EventDto Event { get; init; }
    public required IReadOnlyList<EventOccurrence> Upcoming { get; init; }
    public int UpcomingDays { get; init; }
}
