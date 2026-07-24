using System;
using System.Collections.Generic;
using System.Text;
using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Domain.Entities;


namespace Dsw2026Tpi.Application.Interfaces
{
    public interface ISpecialityService
    {
        Task<Pagination<SpecialityModel.Response>>GetAllSpeciality(int pageSize, int pageIndex, string? name = null);
        Task<SpecialityModel.Response> CreateSpeciality(SpecialityModel.Request Request);
        Task<SpecialityModel.Response> UpdateSpeciality(Guid Id, SpecialityModel.Request Request);
        Task DeleteSpeciality(Guid Id);
    }
}
