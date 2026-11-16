# 02 — Autentikasi admin

Status: done

## Tujuan
Sebagai pengurus, saya ingin login agar hanya admin yang bisa mengelola data jamaah.

## Kriteria penerimaan
- [x] ASP.NET Identity (cookie), `[Authorize]` global
- [x] Hanya Login, AccessDenied, Error yang anonim
- [x] Admin awal dibuat `IdentitySeeder` dari `AdminSeed`
- [x] Kebijakan password dan lockout sesuai arsitektur

## Catatan teknis
- `AccountController`, `IdentitySeeder`, `ApplicationUser`.

## Log dev
Dicatat retroaktif. Belum ada tes otomatis untuk alur login (kandidat story 09).
