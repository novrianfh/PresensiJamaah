# Aplikasi Web Presensi Jamaah

## Ringkasan 
Aplikasi ini adalah sebuah website yang digunakan untuk presensi jamaah masjid dengan menggunakan scan QR Code. Setiap jamaah didaftarkan dan diberikan print-out QR Code. Setiap kajian atau sholat berjamaah, jamaah dapat melakukan scan agar tercatat pada sistem.
Sistem dapat melakukan rekap mingguan, bulanan, tahunan untuk pengolahan data jamaah aktif.


## Spesifikasi
- AspNet MVC dengan Clean Architecture dan .Net versi 10
- Database PostgreSQL

## Model
- Jamaah
  - Nama
  - Nomor HP
  - Alamat
  - Tanggal Lahir
  - Jenis Kelamin
  - Nomor Jamaah (digunakan untuk QR Code)

- Event
 - Nama
 - Deskripsi
 - Waktu Mulai
 - Waktu Selesai
 - Recurring (tiap hari/tiap minggu)
 
 