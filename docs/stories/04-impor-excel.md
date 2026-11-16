# 04 — Impor jamaah dari Excel

Status: done

## Tujuan
Sebagai pengurus, saya ingin mengimpor banyak jamaah dari Excel agar tidak input satu per satu.

## Kriteria penerimaan
- [x] Template `.xlsx` dapat diunduh
- [x] All-or-nothing; semua baris salah dilaporkan dengan nomor baris
- [x] Duplikat dilewati (nama + nomor HP ternormalisasi)
- [x] Batas 2 MB / 1000 baris; satu transaksi

## Catatan teknis
- `JamaahExcel` (Infrastructure), `JamaahImportService` (Application).

## Log dev
Dicatat retroaktif. Tes: `JamaahImportServiceTests`, `JamaahExcelTests`.
