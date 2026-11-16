using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using PresensiJamaah.Application.Common;
using PresensiJamaah.Application.Models;
using PresensiJamaah.Domain.Entities;
using PresensiJamaah.Domain.Enums;

namespace PresensiJamaah.Web.Models;

public static class JenisKelaminExtensions
{
    public static string ToLabel(this JenisKelamin jenisKelamin) => jenisKelamin switch
    {
        JenisKelamin.LakiLaki => "Laki-laki",
        JenisKelamin.Perempuan => "Perempuan",
        _ => "-"
    };

    public static IEnumerable<SelectListItem> ToSelectList(JenisKelamin? selected = null) =>
        Enum.GetValues<JenisKelamin>()
            .Select(v => new SelectListItem(v.ToLabel(), v.ToString(), v == selected));
}

public class JamaahFormViewModel : IValidatableObject
{
    public Guid? Id { get; set; }

    /// <summary>Hanya tampil (read-only) saat ubah data; tidak pernah diterima dari form.</summary>
    public string? NomorJamaah { get; set; }

    [Required(ErrorMessage = "Nama wajib diisi")]
    [StringLength(Jamaah.NamaMaxLength, ErrorMessage = "Nama maksimal {1} karakter")]
    public string Nama { get; set; } = string.Empty;

    [Display(Name = "Nomor HP")]
    [Required(ErrorMessage = "Nomor HP wajib diisi")]
    [RegularExpression(Jamaah.NomorHpPattern,
        ErrorMessage = "Nomor HP tidak valid (contoh: 081234567890)")]
    public string NomorHp { get; set; } = string.Empty;

    [Required(ErrorMessage = "Alamat wajib diisi")]
    [StringLength(Jamaah.AlamatMaxLength, ErrorMessage = "Alamat maksimal {1} karakter")]
    public string Alamat { get; set; } = string.Empty;

    [Display(Name = "Tanggal Lahir")]
    [Required(ErrorMessage = "Tanggal lahir wajib diisi")]
    [DataType(DataType.Date)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public DateOnly? TanggalLahir { get; set; }

    [Display(Name = "Jenis Kelamin")]
    [Required(ErrorMessage = "Jenis kelamin wajib dipilih")]
    public JenisKelamin? JenisKelamin { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TanggalLahir is { } tanggal)
        {
            if (tanggal > DateOnly.FromDateTime(DateTime.Today))
                yield return new ValidationResult("Tanggal lahir tidak boleh di masa depan", [nameof(TanggalLahir)]);
            else if (tanggal.Year < 1900)
                yield return new ValidationResult("Tanggal lahir tidak valid", [nameof(TanggalLahir)]);
        }
    }

    public IEnumerable<SelectListItem> JenisKelaminOptions => JenisKelaminExtensions.ToSelectList(JenisKelamin);

    /// <summary>Hanya dipanggil setelah ModelState valid.</summary>
    public JamaahInput ToInput() => new(
        Nama.Trim(),
        Jamaah.NormalizeNomorHp(NomorHp),
        Alamat.Trim(),
        TanggalLahir!.Value,
        JenisKelamin!.Value);

    public static JamaahFormViewModel From(JamaahDto dto) => new()
    {
        Id = dto.Id,
        NomorJamaah = dto.NomorJamaah,
        Nama = dto.Nama,
        NomorHp = dto.NomorHp,
        Alamat = dto.Alamat,
        TanggalLahir = dto.TanggalLahir,
        JenisKelamin = dto.JenisKelamin
    };
}

public class JamaahImportViewModel
{
    /// <summary>Pesan bila file ditolak sebelum diproses (format/ukuran/header).</summary>
    public string? ErrorFile { get; set; }

    /// <summary>Hasil pemrosesan; null sebelum ada file yang diunggah.</summary>
    public JamaahImportResult? Result { get; set; }
}

public class JamaahIndexViewModel
{
    public string? Search { get; set; }
    public JamaahStatusFilter Status { get; set; } = JamaahStatusFilter.Aktif;
    public PagedResult<JamaahDto> Result { get; set; } = new([], 1, 20, 0);
}
