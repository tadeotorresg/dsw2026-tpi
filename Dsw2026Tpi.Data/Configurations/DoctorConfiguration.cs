using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctors");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(100);

        builder.Property(d => d.LicenseNumber)
            .IsUnicode(false)
            .HasMaxLength(50);

        builder.Property(d => d.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasOne(d => d.Specialty)
            .WithMany()
            .HasForeignKey(d => d.SpecialtyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(d => d.Deleted)
            .HasDefaultValue(false);
        builder.HasQueryFilter(d => !d.Deleted);

        builder.Property(d => d.CreatedAt);
        builder.Property(d => d.UpdatedAt);
    }
}