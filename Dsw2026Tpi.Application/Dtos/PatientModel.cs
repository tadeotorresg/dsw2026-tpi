namespace Dsw2026Tpi.Application.Dtos;

public class PatientModel
{
    public record Request(string? FullName);
    public record Response(Guid Id, long Dni, string? FullName);
}
