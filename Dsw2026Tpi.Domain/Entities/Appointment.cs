using Dsw2026Tpi.Domain.Enums;

namespace Dsw2026Tpi.Domain.Entities;

public class Appointment : EntityBase
{
    public Guid AvailabilitySlotId { get; init; }
    public AvailabilitySlot? AvailabilitySlot { get; private set; }
    public Guid PatientId { get; init; }
    public Patient? Patient { get; private set; }
    public string Reason { get; init; }
    public AppointmentStatus Status { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public DateTime? AttendedAt { get; private set; }

    #region Constructor for EF
#pragma warning disable CS8618
    private Appointment() { }
#pragma warning restore CS8618
    #endregion

    public Appointment(Guid availabilitySlotId, Guid patientId, string reason, Guid? id = null): base(id)
    {
        AvailabilitySlotId = availabilitySlotId;
        PatientId = patientId;
        Reason = reason;
        Status = AppointmentStatus.BOOKED;
    }

    public bool TryCancel()
    {
        if (Status != AppointmentStatus.BOOKED)
            return false;
        var slot = AvailabilitySlot
        ?? throw new InvalidOperationException("Debe cargar el slot asociado para cancelar la cita.");
        var now = DateTime.Now;

        CancelledAt = now;
        Status = AppointmentStatus.CANCELLED;

        slot.Free();
        slot.UpdatedAt = now;

        return true;
    }

    public void MarkAsAttended()
    {
        AttendedAt = DateTime.Now;
        Status = AppointmentStatus.ATTENDED;
    }

    public void MarkAsNoShow()
    {
        Status = AppointmentStatus.NO_SHOW;
    }
}
