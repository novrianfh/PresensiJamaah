# 05 — QR Code dan cetak kartu jamaah

Status: done

## Tujuan
Sebagai pengurus, saya ingin mencetak QR Code tiap jamaah agar jamaah bisa discan saat presensi.

## Kriteria penerimaan
- [x] `IQrCodeGenerator` di Application, implementasi di Infrastructure (isi QR = Nomor Jamaah)
- [x] Halaman detail jamaah menampilkan QR
- [x] Halaman cetak kartu: satu jamaah dan banyak jamaah (aktif) sekaligus, siap print (CSS print)
- [x] Tes: generator menghasilkan gambar tidak kosong untuk Nomor Jamaah valid

## Catatan teknis
- Pustaka QR di Infrastructure saja (mis. QRCoder). Domain/Application tidak bergantung padanya.
- Sentuh `JamaahController`, view `Details`, view cetak baru.

## Log dev
- Paket `QRCoder` 1.8.1 (PngByteQRCode, tanpa System.Drawing) di Infrastructure; ECC level Q.
- Baru: `IQrCodeGenerator`, `Qr/QrCodeGenerator.cs`, view `Jamaah/Kartu.cshtml`, CSS kartu + `@media print` di `site.css`.
- Diubah: `JamaahService.ListAllAktifAsync`, `JamaahController` (aksi `Qr`, `Kartu`, `KartuSemua`), view `Details` dan `Index`, DI Infrastructure.
- Tes baru: `QrCodeGeneratorTests` (4 kasus), 2 tes `ListAllAktif` di `JamaahServiceTests`.
- `dotnet test`: 64 tes lulus (14 + 30 + 20), solusi (termasuk Web) ter-build.
- Belum diuji manual di browser (butuh PostgreSQL + login admin): tampilan `Details` dan hasil cetak/preview print.

## Review
Lolos. Semua kriteria terpenuhi di kode dan tes. Endpoint `Qr`, `Kartu`, `KartuSemua` berada di bawah `[Authorize]` global, jadi QR tidak terbuka untuk anonim. Catatan: daftar kartu massal memuat satu `<img>` per jamaah; untuk ribuan jamaah cukup lambat, tapi bisa diterima untuk skala masjid.
