using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PresensiJamaah.Domain.Entities;

namespace PresensiJamaah.Infrastructure.Persistence.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Event");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Nama).HasMaxLength(Event.NamaMaxLength).IsRequired();
        builder.Property(e => e.Deskripsi).HasMaxLength(Event.DeskripsiMaxLength).IsRequired();
        builder.Property(e => e.WaktuMulai).HasColumnType("timestamp without time zone").IsRequired();
        builder.Property(e => e.WaktuSelesai).HasColumnType("timestamp without time zone").IsRequired();
        builder.Property(e => e.Recurrence).HasConversion<int>().IsRequired();
        builder.Property(e => e.RecurrenceEndDate).HasColumnType("date");

        builder.Ignore(e => e.TanggalMulai);
        builder.HasIndex(e => e.Nama);
        builder.HasIndex(e => e.WaktuMulai);
    }
}
