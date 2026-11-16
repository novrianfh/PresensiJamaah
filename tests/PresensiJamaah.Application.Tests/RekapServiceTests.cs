using PresensiJamaah.Application.Common;
using PresensiJamaah.Application.Models;
using PresensiJamaah.Application.Services;
using PresensiJamaah.Domain.Entities;
using PresensiJamaah.Domain.Enums;

namespace PresensiJamaah.Application.Tests;

public class RekapServiceTests
{
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    // Kamis 2026-11-05 20:00 WIB.
    private static readonly DateTime Sekarang = new(2026, 11, 5, 20, 0, 0);

    private sealed class Fixture
    {
        public FakeJamaahRepository JamaahRepo { get; } = new();
        public FakeEventRepository Events { get; } = new();
        public FakePresensiRepository PresensiRepo { get; } = new();
        public RekapService Service { get; }

        public Fixture(decimal minimal = 50m, DateTime? sekarang = null)
        {
            var clock = new LocalClock(
                new FixedTimeProvider(new DateTimeOffset((sekarang ?? Sekarang).AddHours(-7), TimeSpan.Zero)), "Asia/Jakarta");
            Service = new RekapService(
                new JamaahService(JamaahRepo), Events, PresensiRepo, clock, new RekapOptions(minimal));
        }

        public Jamaah TambahJamaah(string nama, bool aktif = true)
        {
            var j = Jamaah.Create(
                Jamaah.FormatNomorJamaah(JamaahRepo.Data.Count + 1), nama, "081234567890", "Jl. A",
                new DateOnly(1990, 1, 1), JenisKelamin.LakiLaki);
            if (!aktif) j.Nonaktifkan();
            JamaahRepo.Data.Add(j);
            return j;
        }

        /// <summary>Event harian pukul 18:00–19:00 mulai 1 November 2026.</summary>
        public Event TambahEventHarian(string nama = "Maghrib")
        {
            var e = Event.Create(nama, null, new DateTime(2026, 11, 1, 18, 0, 0), new DateTime(2026, 11, 1, 19, 0, 0),
                Recurrence.Harian, null);
            Events.Data.Add(e);
            return e;
        }

        public void Hadir(Jamaah j, Event e, params int[] tanggalNov)
        {
            foreach (var d in tanggalNov)
                PresensiRepo.Data.Add(Presensi.Create(j.Id, e.Id, new DateOnly(2026, 11, d), new DateTime(2026, 11, d, 18, 5, 0)));
        }
    }

    // ---- Rentang periode ----

    [Theory]
    [InlineData("2026-11-05", "2026-11-02", "2026-11-08")] // Kamis → Senin–Minggu
    [InlineData("2026-11-02", "2026-11-02", "2026-11-08")] // Senin
    [InlineData("2026-11-08", "2026-11-02", "2026-11-08")] // Minggu tetap di minggu yang sama
    [InlineData("2026-12-31", "2026-12-28", "2027-01-03")] // melewati pergantian tahun
    public void HitungRentang_Minggu_SeninSampaiMinggu(string tanggal, string dari, string sampai)
    {
        var (d, s) = RekapService.HitungRentang(RekapPeriode.Minggu, DateOnly.Parse(tanggal));

        Assert.Equal(DateOnly.Parse(dari), d);
        Assert.Equal(DateOnly.Parse(sampai), s);
    }

    [Theory]
    [InlineData("2026-11-15", "2026-11-01", "2026-11-30")]
    [InlineData("2028-02-10", "2028-02-01", "2028-02-29")] // kabisat
    public void HitungRentang_Bulan(string tanggal, string dari, string sampai)
    {
        var (d, s) = RekapService.HitungRentang(RekapPeriode.Bulan, DateOnly.Parse(tanggal));

        Assert.Equal(DateOnly.Parse(dari), d);
        Assert.Equal(DateOnly.Parse(sampai), s);
    }

    [Fact]
    public void HitungRentang_Tahun()
    {
        var (d, s) = RekapService.HitungRentang(RekapPeriode.Tahun, new DateOnly(2026, 6, 15));

        Assert.Equal(new DateOnly(2026, 1, 1), d);
        Assert.Equal(new DateOnly(2026, 12, 31), s);
    }

    [Fact]
    public void HitungRentang_PeriodeTidakValid_Melempar() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RekapService.HitungRentang((RekapPeriode)99, new DateOnly(2026, 1, 1)));

    // ---- Persentase dan batas aktif ----

    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(5, 10, 50)]
    [InlineData(10, 10, 100)]
    [InlineData(1, 3, 33.3333333)]
    [InlineData(12, 10, 100)] // melebihi total → dibatasi 100
    [InlineData(3, 0, 0)]     // tanpa kejadian
    public void HitungPersen(int hadir, int total, double expected)
    {
        Assert.Equal((decimal)expected, RekapService.HitungPersen(hadir, total), 5);
    }

    [Fact]
    public void HitungPersen_HadirNegatif_Melempar() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => RekapService.HitungPersen(-1, 10));

    [Theory]
    [InlineData(5, 10, 50, true)]    // tepat di batas → aktif
    [InlineData(4, 10, 50, false)]   // di bawah batas
    [InlineData(6, 10, 50, true)]
    [InlineData(2, 3, 66.67, false)] // 66,666…% < 66,67 (tanpa pembulatan)
    [InlineData(0, 10, 0, true)]     // batas 0 → semua aktif selama ada kejadian
    [InlineData(0, 0, 0, false)]     // tanpa kejadian tidak pernah aktif
    [InlineData(3, 0, 50, false)]
    [InlineData(1, 3, 33.33, true)]  // 33,333…% >= 33,33
    [InlineData(1, 3, 33.34, false)] // 33,333…% < 33,34
    [InlineData(12, 10, 100, true)]  // hadir melebihi total dibatasi 100%
    public void IsAktif_BatasMinimal(int hadir, int total, double minimal, bool expected)
    {
        Assert.Equal(expected, RekapService.IsAktif(hadir, total, (decimal)minimal));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(100.1)]
    public void RekapOptions_DiLuar0Sampai100_Melempar(double nilai) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new RekapOptions((decimal)nilai));

    // ---- RekapAsync ----

    [Fact]
    public async Task Rekap_Bulanan_MenghitungHadirPersenDanStatusAktif()
    {
        var f = new Fixture(minimal: 50);
        var e = f.TambahEventHarian();
        var rajin = f.TambahJamaah("Rajin");
        var pas = f.TambahJamaah("Pas");
        var jarang = f.TambahJamaah("Jarang");
        var nol = f.TambahJamaah("Nol");
        // 1–5 Nov: kejadian 1..4 sudah lewat, tanggal 5 pukul 18:00 sudah dimulai (sekarang 20:00) → total 5.
        f.Hadir(rajin, e, 1, 2, 3, 4, 5);
        f.Hadir(pas, e, 1, 2, 3);   // 3/5 = 60%
        f.Hadir(jarang, e, 5);      // 1/5 = 20%

        var rekap = await f.Service.RekapAsync(new RekapQuery(RekapPeriode.Bulan, new DateOnly(2026, 11, 5)));

        Assert.NotNull(rekap);
        Assert.Equal(new DateOnly(2026, 11, 1), rekap.Dari);
        Assert.Equal(new DateOnly(2026, 11, 30), rekap.Sampai);
        Assert.Equal(5, rekap.TotalKejadian);
        Assert.Equal(["Rajin", "Pas", "Jarang", "Nol"], rekap.Rows.Select(r => r.Nama).ToArray());

        var r1 = rekap.Rows[0];
        Assert.Equal((5, 100m, true), (r1.Hadir, r1.Persen, r1.Aktif));
        var r2 = rekap.Rows[1];
        Assert.Equal((3, 60m, true), (r2.Hadir, r2.Persen, r2.Aktif));
        var r3 = rekap.Rows[2];
        Assert.Equal((1, 20m, false), (r3.Hadir, r3.Persen, r3.Aktif));
        var r4 = rekap.Rows[3];
        Assert.Equal((0, 0m, false), (r4.Hadir, r4.Persen, r4.Aktif));
        Assert.Equal(2, rekap.JumlahAktif);
    }

    [Fact]
    public async Task Rekap_KejadianBelumDimulaiTidakDihitung()
    {
        // Pukul 17:00 Kamis: kejadian hari ini (18:00) belum dimulai → total 1–4 = 4.
        var f = new Fixture(sekarang: new DateTime(2026, 11, 5, 17, 0, 0));
        f.TambahEventHarian();
        f.TambahJamaah("A");

        var rekap = await f.Service.RekapAsync(new RekapQuery(RekapPeriode.Bulan, new DateOnly(2026, 11, 5)));

        Assert.Equal(4, rekap!.TotalKejadian);
    }

    [Fact]
    public async Task Rekap_Mingguan_HanyaMemakaiRentangMinggu()
    {
        var f = new Fixture();
        var e = f.TambahEventHarian();
        var a = f.TambahJamaah("A");
        f.Hadir(a, e, 1, 2, 3, 4); // 2–4 Nov di minggu ini (Senin 2 Nov); 1 Nov (Minggu) minggu lalu

        var rekap = await f.Service.RekapAsync(new RekapQuery(RekapPeriode.Minggu, new DateOnly(2026, 11, 5)));

        Assert.Equal(new DateOnly(2026, 11, 2), rekap!.Dari);
        Assert.Equal(4, rekap.TotalKejadian); // 2,3,4,5 Nov
        Assert.Equal(3, rekap.Rows[0].Hadir);
        Assert.Equal(75m, rekap.Rows[0].Persen);
    }

    [Fact]
    public async Task Rekap_FilterEvent_HanyaMenghitungEventItu()
    {
        var f = new Fixture();
        var maghrib = f.TambahEventHarian("Maghrib");
        var isya = f.TambahEventHarian("Isya");
        var a = f.TambahJamaah("A");
        f.Hadir(a, maghrib, 1, 2, 3, 4, 5);
        f.Hadir(a, isya, 1);

        var semua = await f.Service.RekapAsync(new RekapQuery(RekapPeriode.Bulan, new DateOnly(2026, 11, 5)));
        var hanyaIsya = await f.Service.RekapAsync(new RekapQuery(RekapPeriode.Bulan, new DateOnly(2026, 11, 5), isya.Id));

        Assert.Equal(10, semua!.TotalKejadian);
        Assert.Equal(6, semua.Rows[0].Hadir);
        Assert.Equal("Isya", hanyaIsya!.EventNama);
        Assert.Equal(5, hanyaIsya.TotalKejadian);
        Assert.Equal(1, hanyaIsya.Rows[0].Hadir);
        Assert.Equal(20m, hanyaIsya.Rows[0].Persen);
    }

    [Fact]
    public async Task Rekap_EventTidakDitemukan_Null()
    {
        var f = new Fixture();

        var rekap = await f.Service.RekapAsync(new RekapQuery(RekapPeriode.Bulan, new DateOnly(2026, 11, 5), Guid.NewGuid()));

        Assert.Null(rekap);
    }

    [Fact]
    public async Task Rekap_JamaahNonaktifTidakMasukDaftar()
    {
        var f = new Fixture();
        f.TambahEventHarian();
        f.TambahJamaah("Aktif");
        f.TambahJamaah("Nonaktif", aktif: false);

        var rekap = await f.Service.RekapAsync(new RekapQuery(RekapPeriode.Bulan, new DateOnly(2026, 11, 5)));

        Assert.Equal(["Aktif"], rekap!.Rows.Select(r => r.Nama).ToArray());
    }

    [Fact]
    public async Task Rekap_TanpaKejadian_PersenNolDanTidakAdaYangAktif()
    {
        var f = new Fixture(minimal: 0);
        f.TambahJamaah("A");

        var rekap = await f.Service.RekapAsync(new RekapQuery(RekapPeriode.Bulan, new DateOnly(2026, 11, 5)));

        Assert.Equal(0, rekap!.TotalKejadian);
        Assert.Equal(0m, rekap.Rows[0].Persen);
        Assert.False(rekap.Rows[0].Aktif);
        Assert.Equal(0, rekap.JumlahAktif);
    }

    [Fact]
    public async Task Rekap_Tahunan_MemakaiBatasDariKonfigurasi()
    {
        var f = new Fixture(minimal: 80);
        var e = f.TambahEventHarian();
        var a = f.TambahJamaah("A");
        f.Hadir(a, e, 1, 2, 3, 4); // 4/5 = 80% tepat di batas

        var rekap = await f.Service.RekapAsync(new RekapQuery(RekapPeriode.Tahun, new DateOnly(2026, 11, 5)));

        Assert.Equal(80m, rekap!.MinimalPersen);
        Assert.Equal(new DateOnly(2026, 1, 1), rekap.Dari);
        Assert.True(rekap.Rows[0].Aktif);
    }
}
