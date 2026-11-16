# Arsitektur — Presensi Jamaah

## Lapisan (Clean Architecture, .NET 10)

| Proyek | Tanggung jawab | Bergantung pada |
|---|---|---|
| `PresensiJamaah.Domain` | Entitas, enum, aturan domain | — |
| `PresensiJamaah.Application` | Use case/service, DTO, interface (repository, QR, exporter), validasi | Domain |
| `PresensiJamaah.Infrastructure` | EF Core + Npgsql, migrasi, Identity, generator QR, export Excel | Application |
| `PresensiJamaah.Web` | MVC (Controllers, Razor Views), composition root (`Program.cs`) | Application, Infrastructure |

Aturan: dependensi hanya mengarah ke dalam. Domain tidak mengenal EF Core, ASP.NET, atau pustaka pihak ketiga.

## Model
- **Jamaah**: Id, Nama, NomorHp, Alamat, TanggalLahir, JenisKelamin, NomorJamaah (unik, isi QR, tidak berubah), IsActive.
- **Event**: Id, Nama, Deskripsi, WaktuMulai, WaktuSelesai, Recurrence (`None`/`Harian`/`Mingguan`), RecurrenceEndDate.
- **Presensi**: Id, JamaahId, EventId, TanggalKejadian, WaktuScan. Unique (JamaahId, EventId, TanggalKejadian).

## Pemisahan Akun Login dan Jamaah
- `AspNetUsers` (`ApplicationUser`, di Infrastructure) = admin/pengurus yang login. Role: `Admin`.
- `Jamaah` (entitas Domain) = data jamaah untuk presensi. **Tidak ada FK atau relasi ke `AspNetUsers`**; jamaah tidak punya akun.
- Keduanya berada di `AppDbContext` yang sama, tetapi merupakan tabel terpisah dengan tujuan berbeda.

## Autentikasi
- ASP.NET Identity (cookie), `[Authorize]` global; hanya `Account/Login`, `Account/AccessDenied`, dan `Home/Error` yang anonim.
- Password minimal 8 karakter dengan angka dan huruf kecil; akun terkunci 5 menit setelah 5 kali gagal.
- Admin awal dibuat oleh `IdentitySeeder` dari konfigurasi `AdminSeed` (Email, Password). Isi password lewat user-secrets:
  `dotnet user-secrets set "AdminSeed:Password" "..." --project src/PresensiJamaah.Web`
- Untuk pengembangan, set `AdminSeed:ApplyMigrationsOnStartup=true` agar migrasi dijalankan otomatis saat start.

## Impor Jamaah dari Excel
- Halaman `Jamaah/Import`; template diunduh dari `Jamaah/Template` (dibuat dinamis oleh `JamaahExcel`, ClosedXML).
- Kolom: Nama*, Nomor HP*, Alamat*, Tanggal Lahir* (dd/MM/yyyy), Jenis Kelamin* (Laki-laki/Perempuan). Nomor Jamaah dibuat otomatis.
- Batas: file `.xlsx` maks 2 MB, maks 1000 baris.
- **All-or-nothing**: bila ada satu baris salah, tidak ada yang disimpan dan semua baris salah dilaporkan beserta nomor barisnya.
- **Duplikat dilewati** (nama sama tanpa beda huruf + nomor HP sama, dengan awalan 0/62/+62 dianggap setara), baik terhadap data yang ada maupun di dalam file. Mengunggah ulang file yang sama aman.
- Semua jamaah baru disimpan dalam satu transaksi.
- Pembaca Excel di Infrastructure (`IJamaahExcel`), aturan validasi di Application (`JamaahImportService`).

## Scan Presensi
- Halaman `Presensi/Scan`: kamera aktif otomatis di tengah halaman, tanpa pilihan event dan tanpa input manual. Event dicari otomatis dari waktu scan: yang `Mulai <= sekarang <= Selesai` (waktu lokal; kejadian kemarin ikut dicek untuk event lewat tengah malam). Bila beberapa event berlangsung bersamaan, kehadiran dicatat pada semuanya.
- Isi QR = `NomorJamaah`; pembaca QR sisi klien `html5-qrcode` (`wwwroot/lib`). Setelah scan tampil notifikasi besar: nama jamaah, ucapan terima kasih, dan nama event tempat kehadiran tercatat.
- Hasil scan: `Berhasil`, `SudahTercatat`, `NomorKosong`, `JamaahTidakDitemukan`, `JamaahNonaktif`, `EventTidakBerlangsung`.
- Anti-duplikat berlapis: pengecekan `ExistsAsync` + unique index (JamaahId, EventId, TanggalKejadian); pelanggaran unique akibat scan bersamaan diperlakukan sebagai `SudahTercatat`.
- FK Presensi ke Jamaah dan Event `Restrict`; event yang sudah punya presensi tidak bisa dihapus (jamaah cukup dinonaktifkan).

## Rekap & Jamaah Aktif
- Halaman `Rekap` (filter periode Minggu/Bulan/Tahun, tanggal acuan, event) dan ekspor Excel `Rekap/Export`; logika di `RekapService` (Application), pembuat file `IRekapExcel` di Infrastructure.
- Persentase = hadir / jumlah kejadian yang sudah dimulai pada periode × 100 (maks. 100). Jamaah **aktif** bila persentase >= `App:JamaahAktifMinimalPersen` (0–100, default 50); tanpa kejadian tidak ada yang aktif.
- Hanya jamaah aktif (terdaftar) yang masuk rekap; minggu dimulai hari Senin.
- Keterbatasan: belum ada tanggal pendaftaran jamaah, sehingga jamaah baru dihitung terhadap seluruh kejadian periode.

## Keputusan
- Event berulang disimpan sebagai aturan; occurrence dihitung saat dibutuhkan.
- Waktu disimpan sebagai waktu lokal (`timestamp without time zone`); zona waktu dari `App:TimeZoneId` (default `Asia/Jakarta`).
- Login hanya untuk admin/pengurus (ASP.NET Identity). Jamaah tidak punya akun.
- Scan lewat kamera browser (butuh HTTPS atau `localhost`).

## Konfigurasi
- `ConnectionStrings:Default` — PostgreSQL. Jangan commit password asli; gunakan user-secrets (`dotnet user-secrets`) atau variabel lingkungan `ConnectionStrings__Default`.
- `App:TimeZoneId`, `App:JamaahAktifMinimalPersen` — ambang persentase kehadiran untuk status "jamaah aktif".

## Tahapan
Lihat rencana: fondasi → autentikasi → jamaah → QR & cetak → event → scan → rekap → hardening & rilis.
