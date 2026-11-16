# 01 — Fondasi proyek

Status: done

## Tujuan
Sebagai developer, saya ingin kerangka Clean Architecture .NET 10 + PostgreSQL agar fitur bisa ditambah bertahap.

## Kriteria penerimaan
- [x] Solusi berisi proyek Domain, Application, Infrastructure, Web + proyek tes
- [x] Dependensi hanya mengarah ke dalam
- [x] EF Core + Npgsql, `AppDbContext`, migrasi awal
- [x] Konfigurasi lewat user-secrets/env

## Catatan teknis
- Lihat `arsitektur.md` bagian Lapisan dan Konfigurasi.

## Log dev
Dicatat retroaktif: kode sudah ada di repo; `dotnet test` lulus (58 tes).
