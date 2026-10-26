namespace Dsw2026Tpi.Application.Dtos;

public record DoctorModel
{
    public record Request(string Name, string? LicenseNumber, Guid SpecialtyId, bool? IsActive = null);
    public record Response(Guid Id, string Name, string? LicenseNumber, SpecialtyDto? Specialty, bool IsActive);
    public record SpecialtyDto(Guid? Id, string? Name);
}
