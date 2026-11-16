using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PresensiJamaah.Domain.Entities;
using PresensiJamaah.Infrastructure.Identity;
using PresensiJamaah.Infrastructure.Persistence.Configurations;

namespace PresensiJamaah.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Jamaah> Jamaah => Set<Jamaah>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Presensi> Presensi => Set<Presensi>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Tabel Identity (AspNetUsers, dst.) terpisah dari tabel domain (Jamaah, Event, Presensi).
        builder.Entity<ApplicationUser>().Property(u => u.NamaLengkap).HasMaxLength(150).IsRequired();

        builder.HasSequence<long>(JamaahConfiguration.NomorSequenceName).StartsAt(1).IncrementsBy(1);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
