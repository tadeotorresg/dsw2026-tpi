namespace Dsw2026Tpi.Application.Dtos;

public class AvailabilityModel
{
    public record DayRequest(string Day, TimeSpan StartTime, TimeSpan EndTime);
    public record Request(Guid DoctorId, List<DayRequest> Days);
    public record Response(Guid Id, string Day, string StartTime, string EndTime);
}
