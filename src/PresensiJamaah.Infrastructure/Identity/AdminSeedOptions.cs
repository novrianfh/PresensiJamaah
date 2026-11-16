namespace PresensiJamaah.Infrastructure.Identity;

public class AdminSeedOptions
{
    public const string SectionName = "AdminSeed";

    public string Email { get; set; } = string.Empty;
    public string NamaLengkap { get; set; } = "Administrator";

    /// <summary>Isi lewat user-secrets atau variabel lingkungan, jangan ditulis di appsettings.json.</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>Jalankan migrasi database otomatis saat aplikasi start (cocok untuk pengembangan).</summary>
    public bool ApplyMigrationsOnStartup { get; set; }
}
