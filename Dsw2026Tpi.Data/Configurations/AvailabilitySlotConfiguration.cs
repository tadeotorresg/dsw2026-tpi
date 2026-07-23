using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Data;

namespace Dsw2026Tpi.Data.Configurations;

public class AvailabilitySlotConfiguration : IEntityTypeConfiguration<AvailabilitySlot>
{
    public void Configure(EntityTypeBuilder<AvailabilitySlot> builder)
    {
        builder.ToTable("AvailabilitySlots");

        builder.HasKey(sl => sl.Id);

        builder.HasOne(sl => sl.AvailabilityRule)
            .WithMany(rule => rule.Slots)
            .HasForeignKey(sl => sl.AvailabilityRuleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(sl => sl.SlotDate)
            .IsRequired();

        builder.Property(sl => sl.StartTime)
            .IsRequired();

        builder.Property(sl => sl.EndTime)
            .IsRequired();

        builder.Property(sl => sl.Status)
            .IsRequired()
            .HasConversion<string>()
            .IsUnicode(false)
            .HasMaxLength(20);

        builder.HasIndex(sl => new { sl.AvailabilityRuleId, sl.SlotDate, sl.StartTime})
            .IsUnique();

        builder.Property(sl => sl.Deleted)
            .HasDefaultValue(false);

        builder.HasQueryFilter(sl => !sl.Deleted);

    }
}
