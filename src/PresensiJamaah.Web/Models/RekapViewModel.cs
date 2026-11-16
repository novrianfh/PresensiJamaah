using PresensiJamaah.Application.Models;

namespace PresensiJamaah.Web.Models;

public class RekapViewModel
{
    public required RekapQuery Query { get; init; }
    public required RekapResult Rekap { get; init; }
    public required IReadOnlyList<EventDto> Events { get; init; }
}
