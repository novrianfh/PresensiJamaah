using Microsoft.AspNetCore.Identity;

namespace PresensiJamaah.Infrastructure.Identity;

/// <summary>
/// Akun admin/pengurus yang login ke aplikasi (tabel AspNetUsers).
/// Tidak berhubungan dengan entitas Jamaah: jamaah tidak punya akun dan tidak punya relasi ke tabel ini.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string NamaLengkap { get; set; } = string.Empty;
}
