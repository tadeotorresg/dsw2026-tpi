using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    public class AvailabilityRule: EntityBase
    {
        public Guid DoctorId { get; init; }
        public Doctor? Doctor { get; private set; }
        public byte Month {  get; init; }
        public short Year { get; init; }
        public byte DayOfWeek { get; init; }
        public TimeSpan StartTime { get; init; }
        public TimeSpan EndTime { get; init; }


        #region Constructor for EF
#pragma warning disable CS8618
        private AvailabilityRule() { }
#pragma warning restore CS8618
        #endregion

        public AvailabilityRule (Guid doctorId, byte month, short year, byte dayOfWeek, TimeSpan startTime, TimeSpan endTime, Guid? id = null): base(id)
        {
            DoctorId = doctorId;
            Month = month;
            Year = year;
            DayOfWeek = dayOfWeek;
            StartTime = startTime;
            EndTime = endTime;
        }

        public ICollection<AvailabilitySlot> Slots { get; private set; } = new List<AvailabilitySlot>();
    }
}
