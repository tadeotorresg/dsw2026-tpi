namespace Dsw2026Tpi.Application.Dtos;

public record SpecialtyModel
{
    public record Request(String Name, string Description, bool? IsActive = null);
    public record Response(Guid Id, string Name, String Description, bool IsActive);
}
