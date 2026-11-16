using Microsoft.EntityFrameworkCore;
using PresensiJamaah.Application.Abstractions;
using PresensiJamaah.Domain.Entities;
using PresensiJamaah.Domain.Enums;

namespace PresensiJamaah.Infrastructure.Persistence;

public class EventRepository(AppDbContext db) : IEventRepository
{
    public Task<Event?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Set<Event>().FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<Event>> ListAsync(CancellationToken ct = default) =>
        await db.Set<Event>().AsNoTracking().OrderBy(e => e.Nama).ThenBy(e => e.WaktuMulai).ToListAsync(ct);

    public async Task<IReadOnlyList<Event>> ListCandidatesAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var fromDt = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var toExclusive = to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

        return await db.Set<Event>().AsNoTracking()
            .Where(e => e.WaktuMulai < toExclusive &&
                ((e.Recurrence == Recurrence.None && e.WaktuMulai >= fromDt) ||
                 (e.Recurrence != Recurrence.None && (e.RecurrenceEndDate == null || e.RecurrenceEndDate >= from))))
            .ToListAsync(ct);
    }

    public Task<bool> HasPresensiAsync(Guid eventId, CancellationToken ct = default) =>
        db.Set<Presensi>().AnyAsync(p => p.EventId == eventId, ct);

    public async Task AddAsync(Event e, CancellationToken ct = default) => await db.Set<Event>().AddAsync(e, ct);

    public void Remove(Event e) => db.Set<Event>().Remove(e);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
