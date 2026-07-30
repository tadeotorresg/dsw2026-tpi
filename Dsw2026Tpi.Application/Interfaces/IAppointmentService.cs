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
        Task<IEnumerable<AppointmentModel.Response>> GetPatientAppointments(long dni);
        Task CancelAppointment(Guid id);
        Task<IEnumerable<SearchModel.Response>> GetDailyAppointments(DateOnly? date);
        Task<Pagination<SearchModel.Response>> SearchAppointments(SearchModel.Request request);
    }
}
