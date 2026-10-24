namespace Dsw2026Tpi.Application.Dtos;

public class AvailabilitySlotModel
{
    public record Response(
        Guid Id,
        DateOnly Date,
        string StartTime,
        string EndTime);
}