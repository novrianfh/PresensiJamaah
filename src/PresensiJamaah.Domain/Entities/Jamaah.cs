using PresensiJamaah.Domain.Enums;

namespace PresensiJamaah.Domain.Entities;

/// <summary>
/// Data jamaah masjid. Bukan akun login: tidak berhubungan dengan tabel Identity.
/// NomorJamaah dipakai sebagai isi QR Code dan tidak boleh berubah setelah dibuat.
/// </summary>
public class Jamaah
{
    public const int NamaMaxLength = 150;
    public const int NomorHpMaxLength = 20;
    public const int AlamatMaxLength = 500;
    public const int NomorJamaahMaxLength = 20;

    /// <summary>Format nomor HP Indonesia: 08..., 628..., atau +628...</summary>
    public const string NomorHpPattern = @"^(\+62|62|0)8[0-9]{7,12}$";

    /// <summary>Menghapus spasi dan tanda hubung dari nomor HP.</summary>
    public static string NormalizeNomorHp(string? value) =>
        new((value ?? string.Empty).Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray());

    public static bool IsValidNomorHp(string? value) =>
        System.Text.RegularExpressions.Regex.IsMatch(NormalizeNomorHp(value), NomorHpPattern);

    private Jamaah() { } // untuk EF Core

    public Guid Id { get; private set; }
    public string Nama { get; private set; } = string.Empty;
    public string NomorHp { get; private set; } = string.Empty;
    public string Alamat { get; private set; } = string.Empty;
    public DateOnly TanggalLahir { get; private set; }
    public JenisKelamin JenisKelamin { get; private set; }
    public string NomorJamaah { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    public static string FormatNomorJamaah(long urutan)
    {
        if (urutan <= 0) throw new ArgumentOutOfRangeException(nameof(urutan), "Urutan harus lebih besar dari 0.");
        return $"J-{urutan:D6}";
    }

    public static Jamaah Create(
        string nomorJamaah,
        string nama,
        string nomorHp,
        string alamat,
        DateOnly tanggalLahir,
        JenisKelamin jenisKelamin)
    {
        var jamaah = new Jamaah
        {
            Id = Guid.NewGuid(),
            NomorJamaah = Require(nomorJamaah, nameof(nomorJamaah), NomorJamaahMaxLength),
            IsActive = true
        };
        jamaah.Update(nama, nomorHp, alamat, tanggalLahir, jenisKelamin);
        return jamaah;
    }

    public void Update(string nama, string nomorHp, string alamat, DateOnly tanggalLahir, JenisKelamin jenisKelamin)
    {
        if (!Enum.IsDefined(jenisKelamin))
            throw new ArgumentException("Jenis kelamin tidak valid.", nameof(jenisKelamin));

        Nama = Require(nama, nameof(nama), NamaMaxLength);
        NomorHp = Require(nomorHp, nameof(nomorHp), NomorHpMaxLength);
        Alamat = Require(alamat, nameof(alamat), AlamatMaxLength);
        TanggalLahir = tanggalLahir;
        JenisKelamin = jenisKelamin;
    }

    public void Nonaktifkan() => IsActive = false;

    public void Aktifkan() => IsActive = true;

    private static string Require(string value, string paramName, int maxLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            throw new ArgumentException($"{paramName} wajib diisi.", paramName);
        if (trimmed.Length > maxLength)
            throw new ArgumentException($"{paramName} maksimal {maxLength} karakter.", paramName);
        return trimmed;
    }
}
