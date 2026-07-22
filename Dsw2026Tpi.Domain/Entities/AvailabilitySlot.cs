using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Dsw2026Tpi.Domain.Entities
{
    public class AvailabilitySlot : EntityBase
    {
        public Guid AvailabilityRuleId { get; init; }
        public AvailabilityRule? AvailabilityRule { get; private set; }
        public DateTime SlotDate { get; init; }
        public TimeSpan StartTime { get; init; }
        public TimeSpan EndTime { get; init; }
        public string Status { get; private set; }


        #region Constructor for EF
#pragma warning disable CS8618
        private AvailabilitySlot() { }
#pragma warning restore CS8618
        #endregion

        public AvailabilitySlot (Guid availabilityRuleId, DateTime slotDate, TimeSpan startTime, TimeSpan endTime, Guid? id = null): base(id)
        {
            AvailabilityRuleId = availabilityRuleId;
            SlotDate = slotDate;
            StartTime = startTime;
            EndTime = endTime;
            Status = "AVAILABLE";
        }
        public void Book() => Status = "BOOKED";
        public void Free() => Status = "AVAILABLE";
        public void Block() => Status = "BLOCKED";
    }
}
