using PresensiJamaah.Domain.Entities;
using PresensiJamaah.Domain.Enums;

namespace PresensiJamaah.Domain.Tests;

public class EventTests
{
    // 2026-11-02 adalah hari Senin.
    private static readonly DateTime Senin = new(2026, 11, 2, 18, 0, 0);

    private static Event Buat(
        Recurrence recurrence = Recurrence.None, DateOnly? end = null,
        DateTime? mulai = null, DateTime? selesai = null) =>
        Event.Create("  Sholat Maghrib ", " desc ", mulai ?? Senin, selesai ?? Senin.AddMinutes(30), recurrence, end);

    [Fact]
    public void Create_MemangkasTeksDanMengisiDefault()
    {
        var e = Buat();

        Assert.NotEqual(Guid.Empty, e.Id);
        Assert.Equal("Sholat Maghrib", e.Nama);
        Assert.Equal("desc", e.Deskripsi);
        Assert.Equal(DateTimeKind.Unspecified, e.WaktuMulai.Kind);
    }

    [Fact]
    public void Create_DeskripsiNull_MenjadiKosong()
    {
        var e = Event.Create("Kajian", null, Senin, Senin.AddHours(1), Recurrence.None, null);

        Assert.Equal(string.Empty, e.Deskripsi);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NamaKosong_Ditolak(string nama)
    {
        Assert.Throws<ArgumentException>(() =>
            Event.Create(nama, null, Senin, Senin.AddHours(1), Recurrence.None, null));
    }

    [Fact]
    public void Create_WaktuSelesaiTidakSetelahMulai_Ditolak()
    {
        Assert.Throws<ArgumentException>(() => Buat(selesai: Senin));
        Assert.Throws<ArgumentException>(() => Buat(selesai: Senin.AddMinutes(-1)));
    }

    [Fact]
    public void Create_BatasPengulanganSebelumTanggalMulai_Ditolak()
    {
        Assert.Throws<ArgumentException>(() => Buat(Recurrence.Harian, new DateOnly(2026, 11, 1)));
    }

    [Fact]
    public void Create_BerulangLebihDari24Jam_Ditolak()
    {
        Assert.Throws<ArgumentException>(() =>
            Buat(Recurrence.Harian, selesai: Senin.AddHours(25)));
    }

    [Fact]
    public void Create_TidakBerulang_MengabaikanBatasPengulangan()
    {
        var e = Buat(Recurrence.None, new DateOnly(2026, 12, 31));

        Assert.Null(e.RecurrenceEndDate);
    }

    [Fact]
    public void Create_PengulanganTidakValid_Ditolak()
    {
        Assert.Throws<ArgumentException>(() => Buat((Recurrence)99));
    }

    [Fact]
    public void GetOccurrences_TidakBerulang_HanyaPadaTanggalMulai()
    {
        var e = Buat();

        var hasil = e.GetOccurrences(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30));

        var o = Assert.Single(hasil);
        Assert.Equal(new DateOnly(2026, 11, 2), o.Tanggal);
        Assert.Equal(Senin, o.Mulai);
        Assert.Equal(Senin.AddMinutes(30), o.Selesai);
        Assert.Equal(e.Id, o.EventId);
    }

    [Fact]
    public void GetOccurrences_TidakBerulang_DiLuarRentang_Kosong()
    {
        var e = Buat();

        Assert.Empty(e.GetOccurrences(new DateOnly(2026, 11, 3), new DateOnly(2026, 11, 30)));
        Assert.Empty(e.GetOccurrences(new DateOnly(2026, 10, 1), new DateOnly(2026, 11, 1)));
    }

    [Fact]
    public void GetOccurrences_Harian_SetiapHariDalamRentang()
    {
        var e = Buat(Recurrence.Harian);

        var hasil = e.GetOccurrences(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 8));

        Assert.Equal(7, hasil.Count);
        Assert.All(hasil, o => Assert.Equal(new TimeSpan(18, 0, 0), o.Mulai.TimeOfDay));
        Assert.Equal(new DateOnly(2026, 11, 8), hasil[^1].Tanggal);
    }

    [Fact]
    public void GetOccurrences_Harian_RentangDimulaiSebelumEvent_TidakAdaSebelumTanggalMulai()
    {
        var e = Buat(Recurrence.Harian);

        var hasil = e.GetOccurrences(new DateOnly(2026, 10, 28), new DateOnly(2026, 11, 3));

        Assert.Equal([new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 3)], hasil.Select(o => o.Tanggal));
    }

    [Fact]
    public void GetOccurrences_Harian_DibatasiRecurrenceEndDate_Inklusif()
    {
        var e = Buat(Recurrence.Harian, new DateOnly(2026, 11, 4));

        var hasil = e.GetOccurrences(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30));

        Assert.Equal(3, hasil.Count);
        Assert.Equal(new DateOnly(2026, 11, 4), hasil[^1].Tanggal);
    }

    [Fact]
    public void GetOccurrences_Mingguan_HanyaPadaHariYangSama()
    {
        var e = Buat(Recurrence.Mingguan);

        var hasil = e.GetOccurrences(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30));

        Assert.Equal(
            [new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 9), new DateOnly(2026, 11, 16),
             new DateOnly(2026, 11, 23), new DateOnly(2026, 11, 30)],
            hasil.Select(o => o.Tanggal));
        Assert.All(hasil, o => Assert.Equal(DayOfWeek.Monday, o.Tanggal.DayOfWeek));
    }

    [Fact]
    public void GetOccurrences_Mingguan_RentangDimulaiDiTengahPekan_SelarasDenganHariMulai()
    {
        var e = Buat(Recurrence.Mingguan);

        // Mulai Rabu 2026-11-11: kejadian berikutnya Senin 2026-11-16.
        var hasil = e.GetOccurrences(new DateOnly(2026, 11, 11), new DateOnly(2026, 11, 20));

        Assert.Equal([new DateOnly(2026, 11, 16)], hasil.Select(o => o.Tanggal));
    }

    [Fact]
    public void GetOccurrences_Mingguan_RentangDimulaiTepatDiHariKejadian_Termasuk()
    {
        var e = Buat(Recurrence.Mingguan);

        var hasil = e.GetOccurrences(new DateOnly(2026, 11, 9), new DateOnly(2026, 11, 9));

        Assert.Single(hasil);
    }

    [Fact]
    public void GetOccurrences_Mingguan_DibatasiRecurrenceEndDate()
    {
        var e = Buat(Recurrence.Mingguan, new DateOnly(2026, 11, 15));

        var hasil = e.GetOccurrences(new DateOnly(2026, 11, 1), new DateOnly(2026, 12, 31));

        Assert.Equal(2, hasil.Count);
    }

    [Fact]
    public void GetOccurrences_RentangTerbalik_Kosong()
    {
        var e = Buat(Recurrence.Harian);

        Assert.Empty(e.GetOccurrences(new DateOnly(2026, 11, 10), new DateOnly(2026, 11, 1)));
    }

    [Fact]
    public void GetOccurrences_EventMelewatiTengahMalam_SelesaiHariBerikutnya()
    {
        var e = Buat(Recurrence.Harian, mulai: new DateTime(2026, 11, 2, 23, 0, 0), selesai: new DateTime(2026, 11, 3, 1, 0, 0));

        var o = e.GetOccurrences(new DateOnly(2026, 11, 5), new DateOnly(2026, 11, 5)).Single();

        Assert.Equal(new DateTime(2026, 11, 5, 23, 0, 0), o.Mulai);
        Assert.Equal(new DateTime(2026, 11, 6, 1, 0, 0), o.Selesai);
        Assert.Equal(new DateOnly(2026, 11, 5), o.Tanggal);
    }

    [Fact]
    public void Update_MengubahDataDanIdTetap()
    {
        var e = Buat(Recurrence.Harian, new DateOnly(2026, 12, 1));
        var id = e.Id;

        e.Update("Kajian", "baru", Senin, Senin.AddHours(2), Recurrence.None, new DateOnly(2026, 12, 1));

        Assert.Equal(id, e.Id);
        Assert.Equal("Kajian", e.Nama);
        Assert.Equal(Recurrence.None, e.Recurrence);
        Assert.Null(e.RecurrenceEndDate);
    }
}
