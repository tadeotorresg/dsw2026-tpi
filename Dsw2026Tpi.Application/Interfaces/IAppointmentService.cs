using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Interfaces
{
    public interface IAppointmentService
    {
        Task<AppointmentModel.Response> CreateAppointment(AppointmentModel.Request request);
        Task<IEnumerable<AppointmentModel.PatientAppointmentResponse>> GetPatientAppointments(long dni);
        Task CancelAppointment(Guid id);
        Task<IEnumerable<AppointmentModel.SearchResponse>> GetDailyAppointments(DateOnly date);
        Task<Pagination<AppointmentModel.SearchResponse>> SearchAppointments(
            Guid? specialtyId,
            Guid? doctorId,
            string? dni,
            DateOnly? date,
            int pageSize,
            int pageIndex);
    }
}
