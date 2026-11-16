namespace PresensiJamaah.Application.Common;

/// <summary>Pengaturan rekap dari konfigurasi <c>App:JamaahAktifMinimalPersen</c>.</summary>
public class RekapOptions
{
    public const decimal DefaultMinimalPersen = 50m;

    public RekapOptions(decimal jamaahAktifMinimalPersen)
    {
        if (jamaahAktifMinimalPersen is < 0 or > 100)
            throw new ArgumentOutOfRangeException(
                nameof(jamaahAktifMinimalPersen), "App:JamaahAktifMinimalPersen harus antara 0 dan 100.");
        JamaahAktifMinimalPersen = jamaahAktifMinimalPersen;
    }

    /// <summary>Jamaah dianggap aktif bila persentase kehadiran >= nilai ini (0–100).</summary>
    public decimal JamaahAktifMinimalPersen { get; }
}
