namespace Dsw2026Tpi.Application.Dtos;

public class SearchModel
{
    public record Request(Guid? SpecialtyId, Guid? DoctorId, string? Dni, DateOnly? Date, int PageSize = 10, int PageIndex = 1);

    public record Response(Guid AppointmentId,string? Specialty,string Doctor, string PatientDni, DateOnly Date, string StartTime, string EndTime, string Status);
}
