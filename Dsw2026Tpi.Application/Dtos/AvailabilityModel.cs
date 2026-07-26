using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Dtos
{
    public class AvailabilityModel
    {
        public record DayRequest(string Day, TimeSpan StartTime, TimeSpan EndTime);
        public record Request(Guid DoctorId, List<DayRequest> Days);

        public record Response(string Day, string StartTime, string EndTime);
    }
}
