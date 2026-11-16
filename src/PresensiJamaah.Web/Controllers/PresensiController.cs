using Microsoft.AspNetCore.Mvc;
using PresensiJamaah.Application.Services;

namespace PresensiJamaah.Web.Controllers;

public class PresensiController(PresensiService service) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Scan(CancellationToken ct) => View(await service.ListBerlangsungAsync(ct));

    /// <summary>Dipanggil dari halaman scan (fetch). Event dicari otomatis dari waktu scan. HTTP 200 untuk semua hasil domain.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Scan([FromForm] string? nomorJamaah, CancellationToken ct)
    {
        var result = await service.ScanAsync(nomorJamaah, ct);
        return Json(new
        {
            status = result.Status.ToString(),
            berhasil = result.IsBerhasil,
            pesan = result.Pesan,
            nama = result.NamaJamaah,
            nomor = result.NomorJamaah,
            events = result.NamaEvent ?? [],
            waktu = result.WaktuScan?.ToString("HH:mm:ss")
        });
    }
}
