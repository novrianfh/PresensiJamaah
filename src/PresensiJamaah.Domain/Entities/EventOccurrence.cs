namespace PresensiJamaah.Domain.Entities;

/// <summary>Satu kali kejadian sebuah event pada tanggal tertentu. Dihitung, tidak disimpan.</summary>
public record EventOccurrence(Guid EventId, string Nama, DateOnly Tanggal, DateTime Mulai, DateTime Selesai);
