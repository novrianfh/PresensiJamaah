using PresensiJamaah.Domain.Enums;

namespace PresensiJamaah.Application.Models;

public record JamaahDto(
    Guid Id,
    string NomorJamaah,
    string Nama,
    string NomorHp,
    string Alamat,
    DateOnly TanggalLahir,
    JenisKelamin JenisKelamin,
    bool IsActive);

public record JamaahInput(
    string Nama,
    string NomorHp,
    string Alamat,
    DateOnly TanggalLahir,
    JenisKelamin JenisKelamin);

public enum JamaahStatusFilter
{
    Aktif,
    Nonaktif,
    Semua
}

public record JamaahQuery(
    string? Search = null,
    JamaahStatusFilter Status = JamaahStatusFilter.Aktif,
    int Page = 1,
    int PageSize = 20);
