using PresensiJamaah.Application.Common;

namespace PresensiJamaah.Application.Tests;

public class LocalClockTests
{
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    [Fact]
    public void Today_MemakaiZonaWaktuAplikasi_BukanUtc()
    {
        // 2026-11-04 20:00 UTC = 2026-11-05 03:00 WIB (UTC+7).
        var clock = new LocalClock(new FixedTimeProvider(new DateTimeOffset(2026, 11, 4, 20, 0, 0, TimeSpan.Zero)), "Asia/Jakarta");

        Assert.Equal(new DateOnly(2026, 11, 5), clock.Today());
        Assert.Equal(new DateTime(2026, 11, 5, 3, 0, 0), clock.Now());
        Assert.Equal(DateTimeKind.Unspecified, clock.Now().Kind);
    }

    [Fact]
    public void Konstruktor_ZonaTidakDikenal_Melempar()
    {
        Assert.Throws<TimeZoneNotFoundException>(() => new LocalClock(TimeProvider.System, "Bukan/Zona"));
    }
}
