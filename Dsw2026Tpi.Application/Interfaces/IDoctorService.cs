using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IDoctorService
{
    Task<Pagination<DoctorModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null, Guid? specialtyId = null);
    Task<IEnumerable<AvailabilityModel.Response>> GetAvailabilities(Guid doctorId);
    Task<IEnumerable<AvailabilitySlotModel.Response>> GetAvailableSlots(
    Guid doctorId,
    DateOnly? fromDate = null,
    DateOnly? toDate = null);
    Task<DoctorModel.Response> CreateDoctor(DoctorModel.Request request);
    Task <DoctorModel.Response> UpdateDoctor(Guid id, DoctorModel.Request request);
    Task DeleteDoctor(Guid id);
}
