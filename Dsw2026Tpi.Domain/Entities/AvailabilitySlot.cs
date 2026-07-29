using Dsw2026Tpi.Domain.Enums;

namespace Dsw2026Tpi.Domain.Entities
{
    public class AvailabilitySlot : SoftDeletableEntity
    {
        public Guid AvailabilityRuleId { get; init; }
        public AvailabilityRule? AvailabilityRule { get; private set; }
        public DateTime SlotDate { get; init; }
        public TimeSpan StartTime { get; init; }
        public TimeSpan EndTime { get; init; }
        public SlotStatus Status { get; private set; }


        #region Constructor for EF
#pragma warning disable CS8618
        private AvailabilitySlot() { }
#pragma warning restore CS8618
        #endregion

        public AvailabilitySlot(Guid availabilityRuleId, DateTime slotDate, TimeSpan startTime, TimeSpan endTime, Guid? id = null) : base(id)
        {
            AvailabilityRuleId = availabilityRuleId;
            SlotDate = slotDate;
            StartTime = startTime;
            EndTime = endTime;
            Status = SlotStatus.AVAILABLE;
        }
        public void Book() => Status = SlotStatus.BOOKED;
        public void Free() => Status = SlotStatus.AVAILABLE;
        public void Block() => Status = SlotStatus.BLOCKED;
    }
}
