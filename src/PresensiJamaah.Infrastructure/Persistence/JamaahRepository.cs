using Microsoft.EntityFrameworkCore;
using PresensiJamaah.Application.Abstractions;
using PresensiJamaah.Application.Common;
using PresensiJamaah.Application.Models;
using PresensiJamaah.Domain.Entities;
using PresensiJamaah.Infrastructure.Persistence.Configurations;

namespace PresensiJamaah.Infrastructure.Persistence;

public class JamaahRepository(AppDbContext db) : IJamaahRepository
{
    public Task<Jamaah?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Set<Jamaah>().FirstOrDefaultAsync(j => j.Id == id, ct);

    public Task<Jamaah?> GetByNomorJamaahAsync(string nomorJamaah, CancellationToken ct = default) =>
        db.Set<Jamaah>().AsNoTracking().FirstOrDefaultAsync(j => j.NomorJamaah == nomorJamaah, ct);

    public async Task<PagedResult<Jamaah>> ListAsync(JamaahQuery query, CancellationToken ct = default)
    {
        var q = db.Set<Jamaah>().AsNoTracking();

        q = query.Status switch
        {
            JamaahStatusFilter.Aktif => q.Where(j => j.IsActive),
            JamaahStatusFilter.Nonaktif => q.Where(j => !j.IsActive),
            _ => q
        };

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = EscapeLike(query.Search.Trim());
            var pattern = $"%{term}%";
            q = q.Where(j =>
                EF.Functions.ILike(j.Nama, pattern, "\\") ||
                EF.Functions.ILike(j.NomorJamaah, pattern, "\\") ||
                EF.Functions.ILike(j.NomorHp, pattern, "\\"));
        }

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderBy(j => j.Nama).ThenBy(j => j.NomorJamaah)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<Jamaah>(items, query.Page, query.PageSize, total);
    }

    public async Task<IReadOnlyList<(string Nama, string NomorHp)>> ListNamaNomorHpAsync(CancellationToken ct = default)
    {
        var rows = await db.Set<Jamaah>().AsNoTracking()
            .Select(j => new { j.Nama, j.NomorHp })
            .ToListAsync(ct);
        return rows.Select(r => (r.Nama, r.NomorHp)).ToList();
    }

    public async Task<long> NextNomorUrutAsync(CancellationToken ct = default) =>
        await db.Database
            .SqlQueryRaw<long>($"SELECT nextval('{JamaahConfiguration.NomorSequenceName}') AS \"Value\"")
            .SingleAsync(ct);

    public async Task AddAsync(Jamaah jamaah, CancellationToken ct = default) =>
        await db.Set<Jamaah>().AddAsync(jamaah, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
