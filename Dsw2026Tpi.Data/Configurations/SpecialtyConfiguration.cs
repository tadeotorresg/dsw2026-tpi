using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class SpecialtyConfiguration : IEntityTypeConfiguration<Specialty>
{
    public void Configure(EntityTypeBuilder<Specialty> builder)
    {
        builder.ToTable("Specialties");

        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.Name)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(100);

        builder.HasIndex(s => s.Name)
            .IsUnique()
            .HasFilter("[Deleted] = 0");

        builder.Property(s => s.Description)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(100);

        builder.Property(s => s.Deleted)
            .HasDefaultValue(false);
        builder.HasQueryFilter(s => !s.Deleted);

        builder.Property(s => s.CreatedAt);
        builder.Property(s => s.UpdatedAt);
    }
}
