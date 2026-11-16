using Microsoft.EntityFrameworkCore;
using Npgsql;
using PresensiJamaah.Application.Abstractions;
using PresensiJamaah.Domain.Entities;

namespace PresensiJamaah.Infrastructure.Persistence;

public class PresensiRepository(AppDbContext db) : IPresensiRepository
{
    public Task<bool> ExistsAsync(Guid jamaahId, Guid eventId, DateOnly tanggalKejadian, CancellationToken ct = default) =>
        db.Set<Presensi>().AnyAsync(
            p => p.JamaahId == jamaahId && p.EventId == eventId && p.TanggalKejadian == tanggalKejadian, ct);

    public async Task<IReadOnlyDictionary<Guid, int>> CountPerJamaahAsync(
        DateOnly from, DateOnly to, Guid? eventId, CancellationToken ct = default)
    {
        var q = db.Set<Presensi>().AsNoTracking()
            .Where(p => p.TanggalKejadian >= from && p.TanggalKejadian <= to);
        if (eventId is { } id) q = q.Where(p => p.EventId == id);

        var rows = await q.GroupBy(p => p.JamaahId)
            .Select(g => new { JamaahId = g.Key, Jumlah = g.Count() })
            .ToListAsync(ct);
        return rows.ToDictionary(r => r.JamaahId, r => r.Jumlah);
    }

    public async Task<bool> TryAddAsync(Presensi presensi, CancellationToken ct = default)
    {
        db.Set<Presensi>().Add(presensi);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            db.Entry(presensi).State = EntityState.Detached;
            return false;
        }
    }
}
