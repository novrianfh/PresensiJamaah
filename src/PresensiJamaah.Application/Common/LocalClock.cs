namespace PresensiJamaah.Application.Common;

/// <summary>Waktu "sekarang" dalam zona waktu aplikasi (<c>App:TimeZoneId</c>).</summary>
public class LocalClock(TimeProvider timeProvider, string timeZoneId)
{
    private readonly TimeZoneInfo _zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

    public DateTime Now() =>
        DateTime.SpecifyKind(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), _zone).DateTime, DateTimeKind.Unspecified);

    public DateOnly Today() => DateOnly.FromDateTime(Now());
}
