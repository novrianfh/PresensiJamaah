using PresensiJamaah.Domain.Enums;

namespace PresensiJamaah.Application.Models;

public record EventDto(
    Guid Id,
    string Nama,
    string Deskripsi,
    DateTime WaktuMulai,
    DateTime WaktuSelesai,
    Recurrence Recurrence,
    DateOnly? RecurrenceEndDate);

public record EventInput(
    string Nama,
    string? Deskripsi,
    DateTime WaktuMulai,
    DateTime WaktuSelesai,
    Recurrence Recurrence,
    DateOnly? RecurrenceEndDate);
