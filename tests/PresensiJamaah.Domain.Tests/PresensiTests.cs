using PresensiJamaah.Domain.Entities;

namespace PresensiJamaah.Domain.Tests;

public class PresensiTests
{
    [Fact]
    public void Create_MengisiFieldDanId()
    {
        var jamaahId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var p = Presensi.Create(jamaahId, eventId, new DateOnly(2026, 11, 5), new DateTime(2026, 11, 5, 18, 15, 0, DateTimeKind.Utc));

        Assert.NotEqual(Guid.Empty, p.Id);
        Assert.Equal(jamaahId, p.JamaahId);
        Assert.Equal(eventId, p.EventId);
        Assert.Equal(new DateOnly(2026, 11, 5), p.TanggalKejadian);
        Assert.Equal(DateTimeKind.Unspecified, p.WaktuScan.Kind);
    }

    [Fact]
    public void Create_JamaahKosong_Melempar() =>
        Assert.Throws<ArgumentException>(() =>
            Presensi.Create(Guid.Empty, Guid.NewGuid(), new DateOnly(2026, 11, 5), DateTime.Now));

    [Fact]
    public void Create_EventKosong_Melempar() =>
        Assert.Throws<ArgumentException>(() =>
            Presensi.Create(Guid.NewGuid(), Guid.Empty, new DateOnly(2026, 11, 5), DateTime.Now));
}
