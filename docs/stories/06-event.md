# 06 — Kelola event dan jadwal berulang

Status: done

## Tujuan
Sebagai pengurus, saya ingin membuat event kajian/sholat (sekali atau berulang) agar presensi terkait event.

## Kriteria penerimaan
- [x] Entitas Event + migrasi (Recurrence None/Harian/Mingguan, RecurrenceEndDate)
- [x] CRUD event
- [x] Service menghitung occurrence pada rentang tanggal (tidak disimpan)
- [x] Validasi: WaktuSelesai > WaktuMulai; RecurrenceEndDate >= tanggal mulai
- [x] Tes unit untuk perhitungan occurrence harian/mingguan

## Catatan teknis
- Waktu lokal (`timestamp without time zone`), zona dari `App:TimeZoneId`.

## Log dev
- Domain: `Event` (Create/Update tervalidasi, `GetOccurrences(from, to)`), `EventOccurrence`, enum `Recurrence`.
- Application: `IEventRepository`, `EventService` (CRUD + `GetOccurrencesAsync` semua event / satu event, rentang maks 366 hari), `EventModels`, `LocalClock` (hari ini menurut `App:TimeZoneId`, didaftarkan di `Program.cs`).
- Infrastructure: `EventConfiguration`, `EventRepository`, migrasi `AddEvent`.
- Web: `EventController`, view `Event/{Index,Details,Create,Edit,_Form}`, menu "Event". Detail menampilkan jadwal 30 hari ke depan.
- Keputusan: event berulang maksimal berdurasi 24 jam (supaya kejadian tidak tumpang tindih); `RecurrenceEndDate` diabaikan untuk event sekali; kejadian dihitung per tanggal mulai.
- Tes baru: 21 di `EventTests`, 13 di `EventServiceTests`, 2 di `LocalClockTests`. Total 99 tes lulus; solusi ter-build tanpa warning.
- Belum diuji: migrasi belum dijalankan ke PostgreSQL dan halaman belum dicoba di browser. Kueri `ListCandidatesAsync` hanya dicek lewat fake, bukan terhadap Npgsql.
- Tertunda untuk story 07: Event memakai hapus permanen. Saat tabel Presensi dibuat, FK ke Event harus `Restrict` dan `Delete` perlu menolak event yang sudah punya presensi (atau diganti nonaktif).

## Review
Lolos. Semua kriteria terpenuhi di kode dan tes. Semua aksi berada di bawah `[Authorize]` global dan form POST memakai antiforgery.
