using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Dtos
{
    public class AppointmentModel
    {
        public record Request(Guid DoctorId, Guid AvailabilityId, PatientRequest Patient, string Reason);
        public record PatientRequest(long Dni);
        public record Response(Guid Id, string Status, string Reason, DateOnly Date, string StartTime, string EndTime,
                               DoctorDto Doctor, PatientDto Patient);
        public record PatientAppointmentResponse(Guid Id, DateOnly Date, string StartTime, string EndTime, string Status,
                                                 string Reason, DoctorDto Doctor);
        public record SearchResponse(string Speciality, string Doctor, string AvailableTime);
        public record DoctorDto(Guid Id, string Name, SpecialityDto Speciality);
        public record SpecialityDto(Guid Id, string Name);
        public record PatientDto(Guid Id, string Dni);

    }
  

}
