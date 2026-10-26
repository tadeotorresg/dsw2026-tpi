namespace Dsw2026Tpi.Application.Dtos;

public record DashboardModel
{ 
    public record Response(int ActiveDoctors, int ActiveSpecialties, int BookedAppointmentsToday);
}
