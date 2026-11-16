using PresensiJamaah.Application.Abstractions;
using PresensiJamaah.Application.Common;
using PresensiJamaah.Application.Models;
using PresensiJamaah.Domain.Entities;

namespace PresensiJamaah.Application.Services;

public class JamaahService(IJamaahRepository repository)
{
    public const int MaxPageSize = 100;

    public async Task<PagedResult<JamaahDto>> ListAsync(JamaahQuery query, CancellationToken ct = default)
    {
        var normalized = query with
        {
            Search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, MaxPageSize)
        };

        var result = await repository.ListAsync(normalized, ct);
        return new PagedResult<JamaahDto>(
            result.Items.Select(ToDto).ToList(), result.Page, result.PageSize, result.TotalCount);
    }

    /// <summary>Semua jamaah aktif (urut sesuai repository), untuk cetak kartu massal.</summary>
    public async Task<IReadOnlyList<JamaahDto>> ListAllAktifAsync(CancellationToken ct = default)
    {
        var all = new List<JamaahDto>();
        for (var page = 1; ; page++)
        {
            var result = await repository.ListAsync(
                new JamaahQuery(null, JamaahStatusFilter.Aktif, page, MaxPageSize), ct);
            all.AddRange(result.Items.Select(ToDto));
            if (result.Items.Count == 0 || all.Count >= result.TotalCount) break;
        }
        return all;
    }

    public async Task<JamaahDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var jamaah = await repository.GetByIdAsync(id, ct);
        return jamaah is null ? null : ToDto(jamaah);
    }

    public async Task<JamaahDto> CreateAsync(JamaahInput input, CancellationToken ct = default)
    {
        var urutan = await repository.NextNomorUrutAsync(ct);
        var jamaah = Jamaah.Create(
            Jamaah.FormatNomorJamaah(urutan),
            input.Nama, input.NomorHp, input.Alamat, input.TanggalLahir, input.JenisKelamin);

        await repository.AddAsync(jamaah, ct);
        await repository.SaveChangesAsync(ct);
        return ToDto(jamaah);
    }

    /// <returns>Data terbaru, atau null bila jamaah tidak ditemukan. NomorJamaah tidak ikut berubah.</returns>
    public async Task<JamaahDto?> UpdateAsync(Guid id, JamaahInput input, CancellationToken ct = default)
    {
        var jamaah = await repository.GetByIdAsync(id, ct);
        if (jamaah is null) return null;

        jamaah.Update(input.Nama, input.NomorHp, input.Alamat, input.TanggalLahir, input.JenisKelamin);
        await repository.SaveChangesAsync(ct);
        return ToDto(jamaah);
    }

    /// <returns>False bila jamaah tidak ditemukan.</returns>
    public async Task<bool> SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var jamaah = await repository.GetByIdAsync(id, ct);
        if (jamaah is null) return false;

        if (isActive) jamaah.Aktifkan(); else jamaah.Nonaktifkan();
        await repository.SaveChangesAsync(ct);
        return true;
    }

    private static JamaahDto ToDto(Jamaah j) => new(
        j.Id, j.NomorJamaah, j.Nama, j.NomorHp, j.Alamat, j.TanggalLahir, j.JenisKelamin, j.IsActive);
}
