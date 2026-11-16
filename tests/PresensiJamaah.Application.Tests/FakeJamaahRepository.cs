using PresensiJamaah.Application.Abstractions;
using PresensiJamaah.Application.Common;
using PresensiJamaah.Application.Models;
using PresensiJamaah.Domain.Entities;

namespace PresensiJamaah.Application.Tests;

internal sealed class FakeJamaahRepository : IJamaahRepository
{
    private long _urutan;
    public List<Jamaah> Data { get; } = [];
    public int SaveCount { get; private set; }
    public JamaahQuery? LastQuery { get; private set; }

    public Task<Jamaah?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Data.FirstOrDefault(j => j.Id == id));

    public Task<Jamaah?> GetByNomorJamaahAsync(string nomorJamaah, CancellationToken ct = default) =>
        Task.FromResult(Data.FirstOrDefault(j => j.NomorJamaah == nomorJamaah));

    public Task<PagedResult<Jamaah>> ListAsync(JamaahQuery query, CancellationToken ct = default)
    {
        LastQuery = query;
        var items = query.Status switch
        {
            JamaahStatusFilter.Aktif => Data.Where(j => j.IsActive).ToList(),
            JamaahStatusFilter.Nonaktif => Data.Where(j => !j.IsActive).ToList(),
            _ => Data.ToList()
        };
        return Task.FromResult(new PagedResult<Jamaah>(items, query.Page, query.PageSize, items.Count));
    }

    public Task<IReadOnlyList<(string Nama, string NomorHp)>> ListNamaNomorHpAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<(string, string)>>(Data.Select(j => (j.Nama, j.NomorHp)).ToList());

    public Task<long> NextNomorUrutAsync(CancellationToken ct = default) => Task.FromResult(++_urutan);

    public Task AddAsync(Jamaah jamaah, CancellationToken ct = default)
    {
        Data.Add(jamaah);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
