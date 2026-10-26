using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;

namespace Dsw2026Tpi.Application.Interfaces;

public interface ISpecialtyService
{
    Task<Pagination<SpecialtyModel.Response>>GetAllSpecialty(int pageSize, int pageIndex, string? name = null, bool onlyActive = true);
    Task<SpecialtyModel.Response> CreateSpecialty(SpecialtyModel.Request Request);
    Task<SpecialtyModel.Response> UpdateSpecialty(Guid Id, SpecialtyModel.Request Request);
    Task DeleteSpecialty(Guid Id);
}
