using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    public class Appoinment: EntityBase
    {
        public Guid AvailabilitySlotId { get; init; }
        public AvailabilitySlot? AvailabilitySlot { get; private set; }
        public Guid PatientId { get; init; }
        public Patient? Patient { get; private set; }
        public string Reason { get; init; }
        public string Status { get; private set; }
        public DateTime? CancelledAt { get; private set; }
        public DateTime? AttendedAt { get; private set; }


        #region Constructor for EF
#pragma warning disable CS8618
        private Appoinment() { }
#pragma warning restore CS8618
        #endregion

        public Appoinment(Guid availabilitySlotId, Guid patientId, string reason, Guid? id = null): base(id)
        {
            AvailabilitySlotId = availabilitySlotId;
            PatientId = patientId;
            Reason = reason;
            Status = "BOOKED";
        }
        public void Cancel()
        {
            CancelledAt = DateTime.Now;
            Status = "CANCELED";
        }
        public void MarkAsAttended()
        {
            AttendedAt = DateTime.Now;
            Status = "ATTENDED";
        }
        public void MarkAsNoShow()
        {
            Status = "NO_SHOW";
        }

    }
}
