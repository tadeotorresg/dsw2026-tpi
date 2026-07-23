using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;
public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId)
            .IsRequired();
        
        builder.HasIndex(p => p.UserId)
            .IsUnique();

        builder.Property(p => p.Dni)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(10);

        builder.HasIndex(p => p.Dni)
            .IsUnique();
         
        builder.Property(p => p.FullName)
            .IsUnicode(false)
            .HasMaxLength(150);

        builder.Property(p => p.Deleted)
            .HasDefaultValue(false);
        builder.HasQueryFilter(p => !p.Deleted);

        builder.Property(d => d.CreatedAt);
    }
}
