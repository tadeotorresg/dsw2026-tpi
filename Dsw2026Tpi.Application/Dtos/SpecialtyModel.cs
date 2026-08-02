namespace Dsw2026Tpi.Application.Dtos;

public record SpecialtyModel
{
    public record Request(String Name, String Description);

    public record Response(Guid Id, String Name, String Description);
}
