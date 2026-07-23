using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");

        builder.HasKey(a => a.Id);

        builder.HasOne(a => a.AvailabilitySlot)
            .WithMany()
            .HasForeignKey(a => a.AvailabilitySlotId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => a.AvailabilitySlotId)
            .IsUnique()
            .HasFilter("[Status] = 'BOOKED' AND [Deleted] = 0");

        builder.HasOne(a => a.Patient)
            .WithMany()
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(a => a.Reason)
            .IsRequired()
            .IsUnicode(false)
            .HasMaxLength(300);

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .IsUnicode(false)
            .HasMaxLength(20);

        builder.Property(a => a.CancelledAt);
        builder.Property(appointment => appointment.AttendedAt);
        builder.Property(appointment => appointment.CreatedAt);
        builder.Property(appointment => appointment.UpdatedAt);

        builder.Property(appointment => appointment.Deleted)
            .HasDefaultValue(false);
        builder.HasQueryFilter(appointment => !appointment.Deleted);
    }
}
