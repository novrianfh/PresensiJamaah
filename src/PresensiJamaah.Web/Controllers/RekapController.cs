using Microsoft.AspNetCore.Mvc;
using PresensiJamaah.Application.Abstractions;
using PresensiJamaah.Application.Common;
using PresensiJamaah.Application.Models;
using PresensiJamaah.Application.Services;
using PresensiJamaah.Web.Models;

namespace PresensiJamaah.Web.Controllers;

public class RekapController(
    RekapService service, EventService eventService, IRekapExcel excel, LocalClock clock) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        RekapPeriode? periode, DateOnly? tanggal, Guid? eventId, CancellationToken ct)
    {
        var query = BuatQuery(periode, tanggal, eventId);
        var rekap = await service.RekapAsync(query, ct);
        if (rekap is null) return NotFound();

        return View(new RekapViewModel
        {
            Query = query,
            Rekap = rekap,
            Events = await eventService.ListAsync(ct)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Export(
        RekapPeriode? periode, DateOnly? tanggal, Guid? eventId, CancellationToken ct)
    {
        var query = BuatQuery(periode, tanggal, eventId);
        var rekap = await service.RekapAsync(query, ct);
        if (rekap is null) return NotFound();

        var nama = $"rekap-{rekap.Periode.ToString().ToLowerInvariant()}-{rekap.Dari:yyyyMMdd}.xlsx";
        return File(
            excel.Buat(rekap),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            nama);
    }

    private RekapQuery BuatQuery(RekapPeriode? periode, DateOnly? tanggal, Guid? eventId) =>
        new(
            periode is { } p && Enum.IsDefined(p) ? p : RekapPeriode.Bulan,
            tanggal ?? clock.Today(),
            eventId == Guid.Empty ? null : eventId);
}
