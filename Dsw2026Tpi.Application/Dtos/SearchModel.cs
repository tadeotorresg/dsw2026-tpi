namespace Dsw2026Tpi.Application.Dtos;

public class SearchModel
{
    public record Request(Guid? SpecialtyId, Guid? DoctorId, long? Dni, DateOnly? Date, int PageSize = 10, int PageIndex = 1, string? PatientName=null);
    public record Response(Guid AppointmentsId, string AppointmentsStatus, PatientDto? Patient, DoctorDto? Doctor, DateOnly Date, string StartTime, string EndTime);
    public record PatientDto(long Dni, string FullName);
    public record DoctorDto(Guid DoctorId, string Name, SpecialtyDto? Specialty);
    public record SpecialtyDto(Guid SpecialtyId, string Name);
}
