# Presensi Jamaah

Aplikasi web untuk mencatat kehadiran jamaah masjid dengan scan QR Code. Setiap jamaah didaftarkan dan mendapat kartu ber-QR Code. Saat kajian atau sholat berjamaah, QR discan sehingga kehadiran tercatat. Dari data itu, sistem merekap kehadiran mingguan, bulanan, dan tahunan untuk menentukan jamaah aktif.

## Status

| Fitur | Status |
|---|---|
| Fondasi proyek (Clean Architecture, PostgreSQL) | Selesai |
| Login admin/pengurus | Selesai |
| Kelola data jamaah (tambah, lihat, ubah, nonaktifkan) | Selesai |
| Impor jamaah dari Excel | Selesai |
| QR Code dan cetak kartu jamaah | Selesai |
| Kelola event dan jadwal berulang (harian/mingguan) | Selesai |
| Scan QR untuk presensi | Belum |
| Rekap kehadiran dan jamaah aktif | Belum |
| Hardening dan rilis | Belum |

Rincian per fitur ada di [docs/stories/](docs/stories/).

## Teknologi

- .NET 10, ASP.NET Core MVC (Razor Views), Bootstrap 5
- PostgreSQL dengan EF Core + Npgsql
- ASP.NET Identity (cookie) untuk login admin
- QRCoder (pembuat QR), ClosedXML (impor/ekspor Excel), Serilog (logging)
- xUnit untuk pengujian

## Struktur Solusi

```
src/
  PresensiJamaah.Domain          Entitas dan enum (tanpa dependensi luar)
  PresensiJamaah.Application     Service, DTO, interface repository/QR/Excel
  PresensiJamaah.Infrastructure  EF Core, migrasi, Identity, QR, Excel
  PresensiJamaah.Web             Controller, View, Program.cs
tests/
  PresensiJamaah.Domain.Tests
  PresensiJamaah.Application.Tests
  PresensiJamaah.Infrastructure.Tests
docs/
  prd.md          Kebutuhan produk
  arsitektur.md   Keputusan arsitektur
  stories/        Rencana dan status pengerjaan
```

Dependensi hanya mengarah ke dalam: Web → Infrastructure → Application → Domain. Penjelasan lengkap ada di [docs/arsitektur.md](docs/arsitektur.md).

## Prasyarat

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL (lokal atau remote)
- Tool EF Core: `dotnet tool install --global dotnet-ef`

## Menjalankan Secara Lokal

1. **Siapkan database.** Buat database kosong bernama `presensi_jamaah` di PostgreSQL.

2. **Isi konfigurasi rahasia lewat user-secrets.** Jangan menaruh password asli di `appsettings.json`.

   ```bash
   dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=presensi_jamaah;Username=postgres;Password=PASSWORD_ANDA" --project src/PresensiJamaah.Web
   dotnet user-secrets set "AdminSeed:Password" "PasswordAdmin1" --project src/PresensiJamaah.Web
   ```

   Password admin minimal 8 karakter, memuat angka dan huruf kecil.

3. **Terapkan migrasi.** Pilih salah satu:

   ```bash
   dotnet ef database update --project src/PresensiJamaah.Infrastructure --startup-project src/PresensiJamaah.Web
   ```

   atau set `AdminSeed:ApplyMigrationsOnStartup` ke `true` agar migrasi berjalan otomatis saat aplikasi start (untuk pengembangan saja).

4. **Jalankan aplikasi.**

   ```bash
   dotnet run --project src/PresensiJamaah.Web --launch-profile https
   ```

   Buka `https://localhost:7037` (atau `http://localhost:5210`), lalu login dengan email `admin@masjid.local` dan password yang Anda set di langkah 2.

Scan lewat kamera browser membutuhkan HTTPS atau `localhost`.

## Menjalankan dengan Docker (pengembangan)

Cara ini menjalankan web dan PostgreSQL sekaligus, tanpa perlu memasang .NET SDK atau PostgreSQL. Prasyarat: Docker dengan Compose.

```bash
docker compose up --build
```

Buka `http://localhost:5210` dan login dengan `admin@masjid.local` / `Admin12345`. Migrasi dijalankan otomatis dan admin awal dibuat saat start. Perubahan kode di host langsung dimuat ulang oleh `dotnet watch`.

Pengaturan bawaan dapat diganti lewat file `.env` di folder proyek:

| Variabel | Keterangan | Bawaan |
|---|---|---|
| `WEB_PORT` | Port web di host | `5210` |
| `DB_PORT` | Port PostgreSQL di host | `5433` |
| `DB_PASSWORD` | Password PostgreSQL | `postgres_dev` |
| `ADMIN_EMAIL` | Email admin awal | `admin@masjid.local` |
| `ADMIN_PASSWORD` | Password admin awal | `Admin12345` |

Data database disimpan di volume `pgdata`. Untuk mengosongkan database: `docker compose down -v`.

Setup ini hanya untuk pengembangan, bukan untuk produksi.

## Konfigurasi

| Kunci | Keterangan | Bawaan |
|---|---|---|
| `ConnectionStrings:Default` | Connection string PostgreSQL | placeholder `CHANGE_ME` |
| `App:TimeZoneId` | Zona waktu untuk menentukan waktu lokal event | `Asia/Jakarta` |
| `App:JamaahAktifMinimalPersen` | Ambang persentase kehadiran untuk status "jamaah aktif" | `50` |
| `AdminSeed:Email` | Email admin awal | `admin@masjid.local` |
| `AdminSeed:NamaLengkap` | Nama admin awal | `Administrator` |
| `AdminSeed:Password` | Password admin awal. Admin dibuat saat start jika terisi. | kosong |
| `AdminSeed:ApplyMigrationsOnStartup` | Jalankan migrasi otomatis saat start | `false` |

Selain user-secrets, nilai dapat diberikan lewat variabel lingkungan dengan pemisah `__`, misalnya `ConnectionStrings__Default`.

## Fitur yang Sudah Ada

- **Login:** semua halaman wajib login kecuali halaman login, akses ditolak, dan error. Akun terkunci 5 menit setelah 5 kali salah password. Jamaah tidak punya akun.
- **Data jamaah:** daftar berpaginasi, detail, tambah, ubah, dan status aktif/nonaktif. Nomor Jamaah dibuat otomatis, unik, dan tidak berubah.
- **Impor Excel:** unduh template di halaman Impor, isi kolom Nama, Nomor HP, Alamat, Tanggal Lahir (dd/MM/yyyy), dan Jenis Kelamin (Laki-laki/Perempuan).
  - Batas: file `.xlsx` maksimal 2 MB dan 1000 baris.
  - Jika ada satu baris salah, tidak ada yang disimpan dan semua baris salah dilaporkan beserta nomor barisnya.
  - Jamaah yang sudah ada (nama dan nomor HP sama) dilewati, sehingga mengunggah ulang file yang sama aman.
- **Kartu QR:** QR berisi Nomor Jamaah. Kartu dapat dicetak untuk satu jamaah atau semua jamaah aktif sekaligus.
- **Event:** event sekali atau berulang (harian/mingguan, dengan tanggal akhir opsional). Kejadian berulang dihitung saat dibutuhkan, tidak disimpan.

## Pengujian

```bash
dotnet test
```

## Dokumentasi

- [docs/prd.md](docs/prd.md): kebutuhan produk
- [docs/arsitektur.md](docs/arsitektur.md): lapisan, model, autentikasi, dan keputusan teknis
- [docs/stories/](docs/stories/): rencana pengerjaan dan statusnya

## Catatan

Petunjuk deploy produksi belum ditulis. Itu bagian dari tahap *Hardening dan rilis* ([09-hardening-rilis.md](docs/stories/09-hardening-rilis.md)).
