# 07 — Scan QR untuk presensi

Status: done

## Tujuan
Sebagai pengurus, saya ingin memindai QR jamaah lewat kamera browser agar kehadiran tercatat.

## Kriteria penerimaan
- [x] Entitas Presensi + migrasi, unique (JamaahId, EventId, TanggalKejadian)
- [x] Halaman scan: pilih event yang sedang berlangsung, kamera membaca QR
- [x] Scan valid mencatat presensi dan menampilkan nama jamaah
- [x] Scan ganda, jamaah tidak ditemukan/nonaktif, atau event tidak berlangsung → pesan jelas, tanpa duplikat data
- [x] Tes unit untuk aturan di atas

## Catatan teknis
- Butuh HTTPS atau `localhost`. Pustaka pembaca QR sisi klien (mis. html5-qrcode) di wwwroot/lib.
- Endpoint scan dilindungi `[Authorize]`; fallback input manual Nomor Jamaah.

## Log dev
- Domain: `Presensi` (Create tervalidasi; `TanggalKejadian` = tanggal mulai kejadian, bukan tanggal scan).
- Application: `IPresensiRepository` (`ExistsAsync`, `TryAddAsync`), `PresensiService` (`ListBerlangsungAsync`, `ScanAsync`), `ScanResult`/`ScanStatus`. Tambahan: `IJamaahRepository.GetByNomorJamaahAsync`, `IEventRepository.HasPresensiAsync`.
- Aturan "berlangsung": `Mulai <= sekarang <= Selesai` menurut `LocalClock`; kejadian kemarin ikut dicek agar event lewat tengah malam tetap bisa discan.
- Nomor yang dipindai di-trim dan di-uppercase sebelum dicari.
- Duplikat: dicek lewat `ExistsAsync`, dan `TryAddAsync` menangkap pelanggaran unique PostgreSQL (SqlState 23505) bila dua scan berebut → hasilnya `SudahTercatat`, tanpa data ganda.
- Infrastructure: `PresensiConfiguration` (FK ke Jamaah dan Event `Restrict`, unique index), `PresensiRepository`, migrasi `AddPresensi`.
- Tertunda dari story 06 selesai: `EventService.DeleteAsync` melempar `InvalidOperationException` bila event sudah punya presensi; `EventController.Delete` menampilkan pesannya di halaman detail.
- Web: `PresensiController` (GET `Scan` halaman, POST `Scan` JSON + antiforgery), view `Presensi/Scan`, `wwwroot/js/scan.js`, pustaka `wwwroot/lib/html5-qrcode` (v2.3.8), menu "Scan". Jeda 3 detik untuk QR yang sama agar kamera tidak mengirim berulang.
- Tes baru: 16 di `PresensiServiceTests` (+ theory), 3 di `PresensiTests`, 1 di `EventServiceTests`. Total 119 tes lulus; solusi ter-build tanpa warning.
- **Belum diuji**: migrasi belum dijalankan ke PostgreSQL (tidak ada Docker/PostgreSQL di mesin ini), halaman scan dan kamera belum dicoba di browser, dan penangkapan unique violation di `PresensiRepository` hanya terverifikasi lewat kompilasi, bukan terhadap Npgsql.
- Catatan: dotnet-ef global 10.0.12 lebih lama dari runtime 10.0.14 (hanya peringatan).

### Revisi (permintaan user, 2026-11-05)
- Halaman Scan: kamera di tengah dan diperbesar (area baca 80% sisi terpendek, lebar hingga 640 px), aktif otomatis saat halaman dibuka; input manual dan pilihan event dihapus.
- `PresensiService.ScanAsync(nomorJamaah)` kini mencari event sendiri dari waktu scan (tanpa `eventId`). Bila beberapa event berlangsung bersamaan, kehadiran dicatat di semuanya; bila sebagian sudah tercatat, hanya yang baru dicatat. Status `EventTidakDitemukan` dihapus.
- Notifikasi: nama jamaah besar + "Terima kasih sudah melakukan scan. Kehadiran Anda sudah tercatat pada {event}."; tampil 8 detik. Kuning bila sudah tercatat, merah untuk kesalahan.
- Tes diperbarui: 17 kasus di `PresensiServiceTests` (termasuk pemilihan event menurut waktu dan event bertumpuk). Total 165 tes lulus.
- Belum dicoba di browser/kamera sungguhan.

### Revisi notifikasi popup (2026-11-05)
- Notifikasi hasil scan kini berupa popup (modal Bootstrap) di tengah layar: header berwarna (hijau berhasil / kuning sudah tercatat / merah gagal), nama jamaah besar, pesan terima kasih + nama event, bar waktu, dan tombol **OK**.
- Menutup sendiri setelah 5 detik (`POPUP_MS` di `scan.js`), atau manual lewat OK / klik di luar popup / Esc. Scan baru saat popup terbuka menggantikan isinya dan mengulang hitungan 5 detik; scan yang datang saat popup sedang menutup diantrekan lalu ditampilkan.
- Hanya perubahan tampilan (`Scan.cshtml`, `scan.js`); logika service tidak berubah. Belum dicoba di browser.

### Verifikasi event berulang (2026-11-05)
- Event berulang hanya satu baris di tabel `Event`; presensi dibedakan oleh `TanggalKejadian`. Pengecekan duplikat (`ExistsAsync`) dan unique index (`JamaahId, EventId, TanggalKejadian`) sama-sama memuat tanggal, jadi scan di hari berikutnya tidak terblokir oleh scan hari sebelumnya.
- Tes tambahan: harian 5 hari berturut-turut (1 event, 5 presensi), scan ganda di hari yang sama lalu besok tercatat lagi, mingguan (Kamis minggu depan tercatat, Jumat ditolak), dan setelah `RecurrenceEndDate` ditolak. Total 169 tes lulus.
- Sisi klien: jeda anti-kirim-ulang hanya 4 detik, jadi halaman yang dibiarkan terbuka berhari-hari tetap bisa dipakai.

## Review
Lolos. Setiap kriteria punya implementasi dan tes (kecuali bagian kamera/migrasi yang butuh uji manual, lihat di atas). Endpoint di bawah `[Authorize]` global; POST memakai antiforgery lewat header `RequestVerificationToken`. Nama jamaah ditulis ke DOM lewat `textContent` (bukan innerHTML), jadi aman dari XSS.
