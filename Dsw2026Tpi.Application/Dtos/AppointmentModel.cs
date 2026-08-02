namespace Dsw2026Tpi.Application.Dtos;

public class AppointmentModel
{
    public record Request(Guid DoctorId, Guid AvailabilitySlotId, PatientRequest? Patient, string? Reason);
    public record PatientRequest(long Dni);
    public record Response(Guid Id, Guid DoctorId, Guid AvailabilitySlotId, Guid PatientId, DateOnly Date, string StartTime, string EndTime, string Reason, string Status);
}
