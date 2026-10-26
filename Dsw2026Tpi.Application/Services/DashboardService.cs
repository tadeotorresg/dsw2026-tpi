using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Enums;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IPersistence _persistence;

    public DashboardService(IPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<DashboardModel.Response> GetSummary()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var activeDoctors = await _persistence.Count<Doctor>(
            doctor => !doctor.Deleted && doctor.IsActive);

        var activeSpecialties = await _persistence.Count<Specialty>(
            specialty => !specialty.Deleted && specialty.IsActive);

        var todayConfirmedAppointments =
            await _persistence.Count<Appointment>(
                appointment =>
                    appointment.Status == AppointmentStatus.BOOKED &&
                    appointment.AvailabilitySlot!.SlotDate >= today &&
                    appointment.AvailabilitySlot.SlotDate < tomorrow);

        return new DashboardModel.Response(
            activeDoctors,
            activeSpecialties,
            todayConfirmedAppointments);
    }
}
