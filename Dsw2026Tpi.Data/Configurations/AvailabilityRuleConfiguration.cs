using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class AvailabilityRuleConfiguration : IEntityTypeConfiguration<AvailabilityRule>
{
    public void Configure(EntityTypeBuilder<AvailabilityRule> builder)
    {
        builder.ToTable("AvailabilityRules");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.DoctorId)
            .IsRequired();

        builder.HasOne(r => r.Doctor)
            .WithMany()
            .HasForeignKey(r => r.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.Month)
            .IsRequired();

        builder.Property(r => r.Year)
            .IsRequired();
        builder.Property(r => r.DayOfWeek)
            .IsRequired();
        builder.Property(r => r.StartTime)
            .IsRequired();
        builder.Property(r => r.EndTime)
            .IsRequired();

        builder.HasIndex(r => new { r.DoctorId, r.Year, r.Month, r.DayOfWeek, r.StartTime, r.EndTime})
            .IsUnique()
            .HasFilter("[Deleted] = 0");

        builder.Property(r => r.Deleted)
            .HasDefaultValue(false);
        builder.HasQueryFilter(r => !r.Deleted);
    }
}
