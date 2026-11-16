using Microsoft.AspNetCore.Mvc;
using PresensiJamaah.Application.Common;
using PresensiJamaah.Application.Services;
using PresensiJamaah.Web.Models;

namespace PresensiJamaah.Web.Controllers;

public class EventController(EventService service, LocalClock clock) : Controller
{
    private const int UpcomingDays = 30;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct) => View(await service.ListAsync(ct));

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var dto = await service.GetAsync(id, ct);
        if (dto is null) return NotFound();

        var today = clock.Today();
        var upcoming = await service.GetOccurrencesAsync(id, today, today.AddDays(UpcomingDays - 1), ct) ?? [];
        return View(new EventDetailsViewModel { Event = dto, Upcoming = upcoming, UpcomingDays = UpcomingDays });
    }

    [HttpGet]
    public IActionResult Create()
    {
        var start = clock.Now().Date.AddHours(19); // default petang hari ini, bisa diubah di form
        return View(new EventFormViewModel { WaktuMulai = start, WaktuSelesai = start.AddHours(1) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        var created = await service.CreateAsync(model.ToInput(), ct);
        TempData["Success"] = $"Event {created.Nama} dibuat.";
        return RedirectToAction(nameof(Details), new { id = created.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var dto = await service.GetAsync(id, ct);
        return dto is null ? NotFound() : View(EventFormViewModel.From(dto));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EventFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        var updated = await service.UpdateAsync(id, model.ToInput(), ct);
        if (updated is null) return NotFound();

        TempData["Success"] = "Event diperbarui.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            if (!await service.DeleteAsync(id, ct)) return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Success"] = "Event dihapus.";
        return RedirectToAction(nameof(Index));
    }
}
