using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PresensiJamaah.Domain.Entities;

namespace PresensiJamaah.Infrastructure.Persistence.Configurations;

public class JamaahConfiguration : IEntityTypeConfiguration<Jamaah>
{
    public const string NomorSequenceName = "nomor_jamaah_seq";

    public void Configure(EntityTypeBuilder<Jamaah> builder)
    {
        builder.ToTable("Jamaah");
        builder.HasKey(j => j.Id);

        builder.Property(j => j.Nama).HasMaxLength(Jamaah.NamaMaxLength).IsRequired();
        builder.Property(j => j.NomorHp).HasMaxLength(Jamaah.NomorHpMaxLength).IsRequired();
        builder.Property(j => j.Alamat).HasMaxLength(Jamaah.AlamatMaxLength).IsRequired();
        builder.Property(j => j.TanggalLahir).IsRequired();
        builder.Property(j => j.JenisKelamin).HasConversion<int>().IsRequired();
        builder.Property(j => j.NomorJamaah).HasMaxLength(Jamaah.NomorJamaahMaxLength).IsRequired();
        builder.Property(j => j.IsActive).IsRequired();

        builder.HasIndex(j => j.NomorJamaah).IsUnique();
        builder.HasIndex(j => j.Nama);
    }
}
