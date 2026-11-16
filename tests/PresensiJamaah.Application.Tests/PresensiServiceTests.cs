using PresensiJamaah.Application.Common;
using PresensiJamaah.Application.Models;
using PresensiJamaah.Application.Services;
using PresensiJamaah.Domain.Entities;
using PresensiJamaah.Domain.Enums;

namespace PresensiJamaah.Application.Tests;

public class PresensiServiceTests
{
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    // 2026-11-05 (Kamis) 18:15 WIB = 11:15 UTC.
    private static readonly DateTime SekarangLokal = new(2026, 11, 5, 18, 15, 0);

    private static LocalClock ClockPada(DateTime lokal) =>
        new(new FixedTimeProvider(new DateTimeOffset(lokal.AddHours(-7), TimeSpan.Zero)), "Asia/Jakarta");

    private sealed class Fixture
    {
        public FakeJamaahRepository Jamaah { get; } = new();
        public FakeEventRepository Events { get; } = new();
        public FakePresensiRepository Presensi { get; } = new();
        public PresensiService Service { get; }

        public Fixture(DateTime? sekarang = null) =>
            Service = new PresensiService(Jamaah, Events, Presensi, ClockPada(sekarang ?? SekarangLokal));

        public Jamaah TambahJamaah(string nomor = "J-000001", string nama = "Ahmad", bool aktif = true)
        {
            var j = PresensiJamaah.Domain.Entities.Jamaah.Create(
                nomor, nama, "081234567890", "Jl. Mawar", new DateOnly(1990, 1, 1), JenisKelamin.LakiLaki);
            if (!aktif) j.Nonaktifkan();
            Jamaah.Data.Add(j);
            return j;
        }

        public Event TambahEvent(
            string nama = "Maghrib", DateTime? mulai = null, DateTime? selesai = null,
            Recurrence recurrence = Recurrence.None)
        {
            var m = mulai ?? new DateTime(2026, 11, 5, 18, 0, 0);
            var e = Event.Create(nama, null, m, selesai ?? m.AddMinutes(60), recurrence, null);
            Events.Data.Add(e);
            return e;
        }
    }

    [Fact]
    public async Task Scan_Valid_MencatatPresensiPadaEventYangSesuaiWaktu()
    {
        var f = new Fixture();
        var jamaah = f.TambahJamaah();
        var e = f.TambahEvent();

        var result = await f.Service.ScanAsync("J-000001");

        Assert.Equal(ScanStatus.Berhasil, result.Status);
        Assert.True(result.IsBerhasil);
        Assert.Equal("Ahmad", result.NamaJamaah);
        Assert.Equal(["Maghrib"], result.NamaEvent);
        var p = Assert.Single(f.Presensi.Data);
        Assert.Equal(jamaah.Id, p.JamaahId);
        Assert.Equal(e.Id, p.EventId);
        Assert.Equal(new DateOnly(2026, 11, 5), p.TanggalKejadian);
        Assert.Equal(SekarangLokal, p.WaktuScan);
    }

    [Fact]
    public async Task Scan_Berhasil_PesanBerisiTerimaKasihDanNamaEvent()
    {
        var f = new Fixture();
        f.TambahJamaah();
        f.TambahEvent("Kajian Maghrib");

        var result = await f.Service.ScanAsync("J-000001");

        Assert.Contains("Terima kasih", result.Pesan);
        Assert.Contains("sudah tercatat pada Kajian Maghrib", result.Pesan);
    }

    [Fact]
    public async Task Scan_MemilihEventBerdasarkanWaktu_BukanEventLain()
    {
        var f = new Fixture(new DateTime(2026, 11, 5, 19, 45, 0));
        f.TambahJamaah();
        var maghrib = f.TambahEvent("Maghrib", new DateTime(2026, 11, 5, 18, 0, 0), new DateTime(2026, 11, 5, 19, 0, 0));
        var isya = f.TambahEvent("Isya", new DateTime(2026, 11, 5, 19, 30, 0), new DateTime(2026, 11, 5, 20, 30, 0));

        var result = await f.Service.ScanAsync("J-000001");

        Assert.Equal(["Isya"], result.NamaEvent);
        Assert.Equal(isya.Id, Assert.Single(f.Presensi.Data).EventId);
        Assert.NotEqual(maghrib.Id, f.Presensi.Data[0].EventId);
    }

    [Fact]
    public async Task Scan_DuaEventBerlangsungBersamaan_MencatatKeduanya()
    {
        var f = new Fixture();
        f.TambahJamaah();
        f.TambahEvent("Kajian");
        f.TambahEvent("Maghrib");

        var result = await f.Service.ScanAsync("J-000001");

        Assert.Equal(ScanStatus.Berhasil, result.Status);
        Assert.Equal(2, f.Presensi.Data.Count);
        Assert.Equal(["Kajian", "Maghrib"], result.NamaEvent);
        Assert.Contains("Kajian dan Maghrib", result.Pesan);
    }

    [Fact]
    public async Task Scan_SalahSatuEventSudahTercatat_HanyaMencatatYangBaru()
    {
        var f = new Fixture();
        f.TambahJamaah();
        var kajian = f.TambahEvent("Kajian");
        f.TambahEvent("Maghrib");
        f.Presensi.Data.Add(Presensi.Create(f.Jamaah.Data[0].Id, kajian.Id, new DateOnly(2026, 11, 5), SekarangLokal));

        var result = await f.Service.ScanAsync("J-000001");

        Assert.Equal(ScanStatus.Berhasil, result.Status);
        Assert.Equal(["Maghrib"], result.NamaEvent);
        Assert.Equal(2, f.Presensi.Data.Count);
    }

    [Fact]
    public async Task Scan_NomorDenganSpasiDanHurufKecil_Dinormalkan()
    {
        var f = new Fixture();
        f.TambahJamaah();
        f.TambahEvent();

        var result = await f.Service.ScanAsync("  j-000001 ");

        Assert.Equal(ScanStatus.Berhasil, result.Status);
        Assert.Equal("J-000001", result.NomorJamaah);
    }

    [Fact]
    public async Task Scan_Ganda_TidakMenambahDataDanMemberiPesan()
    {
        var f = new Fixture();
        f.TambahJamaah();
        f.TambahEvent();

        await f.Service.ScanAsync("J-000001");
        var kedua = await f.Service.ScanAsync("J-000001");

        Assert.Equal(ScanStatus.SudahTercatat, kedua.Status);
        Assert.False(kedua.IsBerhasil);
        Assert.Contains("sudah tercatat", kedua.Pesan);
        Assert.Equal(["Maghrib"], kedua.NamaEvent);
        Assert.Single(f.Presensi.Data);
    }

    [Fact]
    public async Task Scan_Balapan_ScanLainMenyimpanLebihDulu_DiperlakukanSebagaiGanda()
    {
        var f = new Fixture();
        f.TambahJamaah();
        f.TambahEvent();
        f.Presensi.SimulasiBalapan = true;

        var result = await f.Service.ScanAsync("J-000001");

        Assert.Equal(ScanStatus.SudahTercatat, result.Status);
        Assert.Empty(f.Presensi.Data);
    }

    [Fact]
    public async Task Scan_JamaahTidakDitemukan_TidakMencatat()
    {
        var f = new Fixture();
        f.TambahEvent();

        var result = await f.Service.ScanAsync("J-999999");

        Assert.Equal(ScanStatus.JamaahTidakDitemukan, result.Status);
        Assert.Contains("J-999999", result.Pesan);
        Assert.Empty(f.Presensi.Data);
    }

    [Fact]
    public async Task Scan_JamaahNonaktif_DitolakTanpaMencatat()
    {
        var f = new Fixture();
        f.TambahJamaah(aktif: false);
        f.TambahEvent();

        var result = await f.Service.ScanAsync("J-000001");

        Assert.Equal(ScanStatus.JamaahNonaktif, result.Status);
        Assert.Equal("Ahmad", result.NamaJamaah);
        Assert.Empty(f.Presensi.Data);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Scan_NomorKosong_Ditolak(string? nomor)
    {
        var f = new Fixture();
        f.TambahEvent();

        var result = await f.Service.ScanAsync(nomor);

        Assert.Equal(ScanStatus.NomorKosong, result.Status);
        Assert.Empty(f.Presensi.Data);
    }

    [Fact]
    public async Task Scan_TidakAdaEventBerlangsung_Ditolak()
    {
        var f = new Fixture();
        f.TambahJamaah();

        var result = await f.Service.ScanAsync("J-000001");

        Assert.Equal(ScanStatus.EventTidakBerlangsung, result.Status);
        Assert.Empty(f.Presensi.Data);
    }

    [Theory]
    [InlineData(17, 59)] // semenit sebelum mulai
    [InlineData(19, 1)]  // semenit setelah selesai
    public async Task Scan_DiLuarWaktuEvent_Ditolak(int jam, int menit)
    {
        var f = new Fixture(new DateTime(2026, 11, 5, jam, menit, 0));
        f.TambahJamaah();
        f.TambahEvent(); // 18:00–19:00

        var result = await f.Service.ScanAsync("J-000001");

        Assert.Equal(ScanStatus.EventTidakBerlangsung, result.Status);
        Assert.Empty(f.Presensi.Data);
    }

    [Fact]
    public async Task Scan_EventHariLain_Ditolak()
    {
        var f = new Fixture();
        f.TambahJamaah();
        f.TambahEvent(mulai: new DateTime(2026, 11, 6, 18, 0, 0)); // besok

        var result = await f.Service.ScanAsync("J-000001");

        Assert.Equal(ScanStatus.EventTidakBerlangsung, result.Status);
    }

    [Fact]
    public async Task Scan_EventHarian_MencatatPerTanggalKejadian()
    {
        var f = new Fixture(new DateTime(2026, 11, 5, 18, 15, 0));
        f.TambahJamaah();
        f.TambahEvent(mulai: new DateTime(2026, 11, 1, 18, 0, 0), recurrence: Recurrence.Harian);

        var hariIni = await f.Service.ScanAsync("J-000001");

        Assert.Equal(ScanStatus.Berhasil, hariIni.Status);
        Assert.Equal(new DateOnly(2026, 11, 5), Assert.Single(f.Presensi.Data).TanggalKejadian);

        // Hari berikutnya, jamaah yang sama boleh tercatat lagi (kejadian berbeda).
        var besok = new PresensiService(f.Jamaah, f.Events, f.Presensi, ClockPada(new DateTime(2026, 11, 6, 18, 15, 0)));
        var hasilBesok = await besok.ScanAsync("J-000001");

        Assert.Equal(ScanStatus.Berhasil, hasilBesok.Status);
        Assert.Equal(2, f.Presensi.Data.Count);
    }

    // Satu baris Event di database untuk seluruh pengulangan: presensi dibedakan oleh TanggalKejadian.
    [Fact]
    public async Task Scan_EventHarian_BisaScanSetiapHariBerturut_SatuEventSatuBaris()
    {
        var f = new Fixture();
        f.TambahJamaah();
        var e = f.TambahEvent(mulai: new DateTime(2026, 11, 1, 18, 0, 0), recurrence: Recurrence.Harian);

        for (var hari = 5; hari <= 9; hari++)
        {
            var service = new PresensiService(f.Jamaah, f.Events, f.Presensi, ClockPada(new DateTime(2026, 11, hari, 18, 10, 0)));
            var result = await service.ScanAsync("J-000001");
            Assert.Equal(ScanStatus.Berhasil, result.Status);
        }

        Assert.Single(f.Events.Data);
        Assert.Equal(5, f.Presensi.Data.Count);
        Assert.All(f.Presensi.Data, p => Assert.Equal(e.Id, p.EventId));
        Assert.Equal(
            Enumerable.Range(5, 5).Select(d => new DateOnly(2026, 11, d)),
            f.Presensi.Data.Select(p => p.TanggalKejadian).OrderBy(d => d));
    }

    [Fact]
    public async Task Scan_EventHarian_ScanDuaKaliDiHariYangSama_HanyaSatuTercatatLaluBesokBisaLagi()
    {
        var f = new Fixture();
        f.TambahJamaah();
        f.TambahEvent(mulai: new DateTime(2026, 11, 1, 18, 0, 0), recurrence: Recurrence.Harian);

        var hariIni = new PresensiService(f.Jamaah, f.Events, f.Presensi, ClockPada(new DateTime(2026, 11, 5, 18, 10, 0)));
        Assert.Equal(ScanStatus.Berhasil, (await hariIni.ScanAsync("J-000001")).Status);
        Assert.Equal(ScanStatus.SudahTercatat, (await hariIni.ScanAsync("J-000001")).Status);

        var besok = new PresensiService(f.Jamaah, f.Events, f.Presensi, ClockPada(new DateTime(2026, 11, 6, 18, 10, 0)));
        Assert.Equal(ScanStatus.Berhasil, (await besok.ScanAsync("J-000001")).Status);

        Assert.Equal(2, f.Presensi.Data.Count);
    }

    [Fact]
    public async Task Scan_EventMingguan_MingguDepanBisaScanLagi_HariLainDitolak()
    {
        var f = new Fixture();
        f.TambahJamaah();
        // Kajian tiap Kamis 18:00 (5 Nov 2026 adalah Kamis).
        f.TambahEvent("Kajian Kamis", new DateTime(2026, 11, 5, 18, 0, 0), new DateTime(2026, 11, 5, 19, 0, 0), Recurrence.Mingguan);

        PresensiService Pada(DateTime waktu) => new(f.Jamaah, f.Events, f.Presensi, ClockPada(waktu));

        Assert.Equal(ScanStatus.Berhasil, (await Pada(new DateTime(2026, 11, 5, 18, 10, 0)).ScanAsync("J-000001")).Status);
        Assert.Equal(ScanStatus.SudahTercatat, (await Pada(new DateTime(2026, 11, 5, 18, 40, 0)).ScanAsync("J-000001")).Status);
        // Jumat 18:10 bukan jadwal kajian.
        Assert.Equal(ScanStatus.EventTidakBerlangsung, (await Pada(new DateTime(2026, 11, 6, 18, 10, 0)).ScanAsync("J-000001")).Status);
        // Kamis minggu depan & dua minggu lagi: tercatat lagi.
        Assert.Equal(ScanStatus.Berhasil, (await Pada(new DateTime(2026, 11, 12, 18, 10, 0)).ScanAsync("J-000001")).Status);
        Assert.Equal(ScanStatus.Berhasil, (await Pada(new DateTime(2026, 11, 19, 18, 10, 0)).ScanAsync("J-000001")).Status);

        Assert.Equal(
            [new DateOnly(2026, 11, 5), new DateOnly(2026, 11, 12), new DateOnly(2026, 11, 19)],
            f.Presensi.Data.Select(p => p.TanggalKejadian).OrderBy(d => d));
    }

    [Fact]
    public async Task Scan_EventBerulang_SetelahBatasPengulangan_Ditolak()
    {
        var f = new Fixture();
        f.TambahJamaah();
        var e = Event.Create(
            "Kajian Ramadhan", null, new DateTime(2026, 11, 1, 18, 0, 0), new DateTime(2026, 11, 1, 19, 0, 0),
            Recurrence.Harian, new DateOnly(2026, 11, 4));
        f.Events.Data.Add(e);

        PresensiService Pada(DateTime waktu) => new(f.Jamaah, f.Events, f.Presensi, ClockPada(waktu));

        Assert.Equal(ScanStatus.Berhasil, (await Pada(new DateTime(2026, 11, 4, 18, 10, 0)).ScanAsync("J-000001")).Status);
        Assert.Equal(ScanStatus.EventTidakBerlangsung, (await Pada(new DateTime(2026, 11, 5, 18, 10, 0)).ScanAsync("J-000001")).Status);
        Assert.Single(f.Presensi.Data);
    }

    [Fact]
    public async Task Scan_EventLewatTengahMalam_TanggalKejadianMengikutiTanggalMulai()
    {
        // Event 23:00–01:00, discan pukul 00:30 hari berikutnya.
        var f = new Fixture(new DateTime(2026, 11, 6, 0, 30, 0));
        f.TambahJamaah();
        f.TambahEvent(
            "Qiyamullail", new DateTime(2026, 11, 5, 23, 0, 0), new DateTime(2026, 11, 6, 1, 0, 0), Recurrence.Harian);

        var result = await f.Service.ScanAsync("J-000001");

        Assert.Equal(ScanStatus.Berhasil, result.Status);
        Assert.Equal(new DateOnly(2026, 11, 5), Assert.Single(f.Presensi.Data).TanggalKejadian);
    }

    [Fact]
    public async Task ListBerlangsung_HanyaMengembalikanEventYangSedangBerjalan()
    {
        var f = new Fixture(); // 18:15
        f.TambahEvent("Maghrib", new DateTime(2026, 11, 5, 18, 0, 0), new DateTime(2026, 11, 5, 19, 0, 0));
        f.TambahEvent("Isya", new DateTime(2026, 11, 5, 19, 30, 0), new DateTime(2026, 11, 5, 20, 0, 0));
        f.TambahEvent("Subuh", new DateTime(2026, 11, 1, 4, 30, 0), new DateTime(2026, 11, 1, 5, 0, 0), Recurrence.Harian);
        f.TambahEvent("Kajian Sore", new DateTime(2026, 11, 1, 17, 0, 0), new DateTime(2026, 11, 1, 19, 0, 0), Recurrence.Harian);

        var list = await f.Service.ListBerlangsungAsync();

        Assert.Equal(["Kajian Sore", "Maghrib"], list.Select(o => o.Nama).ToArray());
    }
}
