using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PresensiJamaah.Domain.Entities;

namespace PresensiJamaah.Infrastructure.Persistence.Configurations;

public class PresensiConfiguration : IEntityTypeConfiguration<Presensi>
{
    public void Configure(EntityTypeBuilder<Presensi> builder)
    {
        builder.ToTable("Presensi");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.TanggalKejadian).HasColumnType("date").IsRequired();
        builder.Property(p => p.WaktuScan).HasColumnType("timestamp without time zone").IsRequired();

        // Restrict: jamaah/event yang sudah punya presensi tidak boleh terhapus diam-diam.
        builder.HasOne<Jamaah>().WithMany().HasForeignKey(p => p.JamaahId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Event>().WithMany().HasForeignKey(p => p.EventId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.JamaahId, p.EventId, p.TanggalKejadian }).IsUnique();
        builder.HasIndex(p => new { p.EventId, p.TanggalKejadian });
    }
}
