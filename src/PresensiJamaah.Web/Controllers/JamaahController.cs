using Microsoft.AspNetCore.Mvc;
using PresensiJamaah.Application.Abstractions;
using PresensiJamaah.Application.Models;
using PresensiJamaah.Application.Services;
using PresensiJamaah.Web.Models;

namespace PresensiJamaah.Web.Controllers;

public class JamaahController(
    JamaahService service,
    JamaahImportService importService,
    IQrCodeGenerator qrGenerator,
    ILogger<JamaahController> logger) : Controller
{
    private const int PageSize = 20;
    private const long MaxFileBytes = 2 * 1024 * 1024;
    private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet]
    public IActionResult Import() => View(new JamaahImportViewModel());

    [HttpGet]
    public IActionResult Template() =>
        File(importService.BuatTemplate(), XlsxContentType, "template-impor-jamaah.xlsx");

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxFileBytes + 512 * 1024)]
    public async Task<IActionResult> Import(IFormFile? file, CancellationToken ct)
    {
        var model = new JamaahImportViewModel();

        if (file is null || file.Length == 0)
        {
            model.ErrorFile = "Pilih file Excel (.xlsx) terlebih dahulu.";
            return View(model);
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            model.ErrorFile = "Format file harus .xlsx. Unduh template yang disediakan.";
            return View(model);
        }

        if (file.Length > MaxFileBytes)
        {
            model.ErrorFile = $"Ukuran file maksimal {MaxFileBytes / (1024 * 1024)} MB.";
            return View(model);
        }

        try
        {
            await using var stream = file.OpenReadStream();
            model.Result = await importService.ImportAsync(stream, ct);
            logger.LogInformation("Impor jamaah: {Diimpor} diimpor, {Dilewati} dilewati, {Salah} salah",
                model.Result.Diimpor, model.Result.Dilewati.Count, model.Result.Kesalahan.Count);
        }
        catch (ImportFileException ex)
        {
            model.ErrorFile = ex.Message;
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search, JamaahStatusFilter status = JamaahStatusFilter.Aktif, int page = 1, CancellationToken ct = default)
    {
        var result = await service.ListAsync(new JamaahQuery(search, status, page, PageSize), ct);
        return View(new JamaahIndexViewModel { Search = search, Status = status, Result = result });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var dto = await service.GetAsync(id, ct);
        return dto is null ? NotFound() : View(dto);
    }

    [HttpGet]
    public async Task<IActionResult> Qr(Guid id, CancellationToken ct)
    {
        var dto = await service.GetAsync(id, ct);
        return dto is null ? NotFound() : File(qrGenerator.GeneratePng(dto.NomorJamaah), "image/png");
    }

    [HttpGet]
    public async Task<IActionResult> Kartu(Guid id, CancellationToken ct)
    {
        var dto = await service.GetAsync(id, ct);
        return dto is null ? NotFound() : View("Kartu", (IReadOnlyList<JamaahDto>)[dto]);
    }

    [HttpGet]
    public async Task<IActionResult> KartuSemua(CancellationToken ct) =>
        View("Kartu", await service.ListAllAktifAsync(ct));

    [HttpGet]
    public IActionResult Create() => View(new JamaahFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(JamaahFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        var created = await service.CreateAsync(model.ToInput(), ct);
        TempData["Success"] = $"Jamaah {created.Nama} terdaftar dengan nomor {created.NomorJamaah}.";
        return RedirectToAction(nameof(Details), new { id = created.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var dto = await service.GetAsync(id, ct);
        return dto is null ? NotFound() : View(JamaahFormViewModel.From(dto));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, JamaahFormViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            // NomorJamaah tidak ikut form; ambil ulang agar tetap tampil.
            model.NomorJamaah = (await service.GetAsync(id, ct))?.NomorJamaah;
            return View(model);
        }

        var updated = await service.UpdateAsync(id, model.ToInput(), ct);
        if (updated is null) return NotFound();

        TempData["Success"] = "Data jamaah diperbarui.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Nonaktifkan(Guid id, CancellationToken ct)
    {
        if (!await service.SetActiveAsync(id, false, ct)) return NotFound();
        TempData["Success"] = "Jamaah dinonaktifkan. Data presensi tetap tersimpan.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Aktifkan(Guid id, CancellationToken ct)
    {
        if (!await service.SetActiveAsync(id, true, ct)) return NotFound();
        TempData["Success"] = "Jamaah diaktifkan kembali.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
