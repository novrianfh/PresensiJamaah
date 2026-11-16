# 08 — Rekap kehadiran dan jamaah aktif

Status: done

## Tujuan
Sebagai pengurus, saya ingin rekap mingguan, bulanan, dan tahunan agar tahu jamaah yang aktif.

## Kriteria penerimaan
- [x] Rekap per periode (minggu/bulan/tahun) dan filter event
- [x] Persentase kehadiran per jamaah = hadir / jumlah occurrence pada periode
- [x] Status "jamaah aktif" bila persentase >= `App:JamaahAktifMinimalPersen`
- [x] Ekspor rekap ke Excel
- [x] Tes unit untuk perhitungan persentase dan batas aktif

## Catatan teknis
- Hitung occurrence dari service story 06; data dari tabel Presensi.

## Log dev
- Application: `RekapService` (`RekapAsync`, serta fungsi murni `HitungRentang`, `HitungPersen`, `IsAktif`), `RekapModels` (`RekapPeriode`, `RekapQuery`, `RekapRow`, `RekapResult`), `RekapOptions` (batas aktif, divalidasi 0–100), `IRekapExcel`. Tambahan: `IPresensiRepository.CountPerJamaahAsync`.
- Infrastructure: `PresensiRepository.CountPerJamaahAsync` (GROUP BY jamaah), `RekapExcel` (ClosedXML; teks ditulis dengan `SetValue` + format `@` agar nama seperti `=1+1` tidak menjadi rumus), terdaftar di DI.
- Web: `RekapController` (`Index`, `Export` → `rekap-{periode}-{yyyyMMdd}.xlsx`), `RekapViewModel`, view `Rekap/Index`, menu "Rekap". `RekapOptions` didaftarkan di `Program.cs` dari `App:JamaahAktifMinimalPersen` (nilai di luar 0–100 membuat aplikasi gagal start dengan pesan jelas).
- Keputusan perhitungan:
  - Periode: minggu = Senin–Minggu, bulan kalender, tahun kalender, ditentukan dari "tanggal acuan" (default hari ini, periode default Bulan).
  - Penyebut = kejadian yang **sudah dimulai** (`Mulai <= sekarang`) pada periode; kejadian di masa depan tidak dihitung, jadi periode berjalan tidak merugikan jamaah.
  - Semua event: penyebut = jumlah kejadian semua event; hadir = jumlah presensi jamaah di semua event.
  - Batas aktif dibandingkan tanpa pembulatan (`hadir × 100 >= minimal × total`); tepat di batas = aktif. Tanpa kejadian → persen 0 dan tidak ada yang aktif.
  - Persentase dibatasi 100 (bila event diubah jadwalnya setelah ada presensi, hadir bisa melebihi kejadian).
  - Daftar hanya berisi jamaah **aktif**; jamaah nonaktif tidak ditampilkan. Urutan: persentase turun, lalu nama.
- Keterbatasan yang diketahui: tidak ada tanggal pendaftaran jamaah, jadi jamaah yang baru terdaftar di tengah periode dihitung terhadap seluruh kejadian periode itu.
- Tes baru: 35 di `RekapServiceTests` (rentang, persen, batas aktif, rekap, filter event), 7 di `RekapExcelTests`. Total 161 tes lulus; solusi ter-build tanpa warning.
- **Belum diuji**: halaman Rekap dan unduhan Excel belum dicoba di browser; kueri `CountPerJamaahAsync` belum dijalankan terhadap PostgreSQL (tidak ada Docker/PostgreSQL di mesin ini), hanya dicek lewat fake dan kompilasi.

## Review
Lolos. Setiap kriteria punya implementasi dan tes. Endpoint di bawah `[Authorize]` global; `Export` hanya GET baca-saja (tanpa efek samping). Parameter periode tak valid dikembalikan ke default, event tak dikenal → 404. Ekspor aman dari formula injection (diuji untuk `=`, `+`, `@`).
