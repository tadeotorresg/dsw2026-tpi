using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Dsw2026Tpi.CrossCutting.Helpers;

namespace Dsw2026Tpi.Application.Services
{
    public class SpecialtyService : ISpecialtyService
    {
        private readonly IPersistence _persistence;

        public SpecialtyService (IPersistence persistence)
        {
            _persistence = persistence;

        }

        public async Task<SpecialtyModel.Response> CreateSpecialty(SpecialtyModel.Request request)
        { 
            if (!request.Name.IsNameValid())
                throw new ValidationException()
                    .WithDetail(nameof(request.Name), "Nombre inválido, entre 3 y 100 caracteres.");

            if (!request.Description.IsDescriptionValid())
                throw new ValidationException()
                    .WithDetail(nameof(request.Description), "Descripción inválida, entre 10 y 100 caracteres.");
            
            var specialty = new Specialty(request.Name, request.Description);

            await _persistence.Add(specialty);

            return MapResponse(specialty);
        }

        public async Task<Pagination<SpecialtyModel.Response>> GetAllSpecialty(int pageSize, int pageIndex, string? name = null)
        {
            if (!string.IsNullOrWhiteSpace(name) && !name.IsNameValid())
                throw new ValidationException()
                    .WithDetail(nameof(name), "El filtro por nombre debe tener entre 3 y 100 caracteres.");
            
            var specialties = await _persistence.Paginate<Specialty, string>(pageSize, pageIndex,
                s => string.IsNullOrWhiteSpace(name) || s.Name.Contains(name),
                x => x.Name
            );

            return specialties.Map(MapResponse);
        }

        public async Task<SpecialtyModel.Response> UpdateSpecialty(Guid id, SpecialtyModel.Request request)
        {
            if (!request.Name.IsNameValid())
                throw new ValidationException()
                    .WithDetail(nameof(request.Name), "El nombre es inválido. El campo debe tener entre 3 y 100 caracteres.");

            if (!request.Description.IsDescriptionValid())
                throw new ValidationException()
                    .WithDetail(nameof(request.Description), "La descripción es inválida. El campo debe tener entre 10 y 100 caracteres.");

            var specialty = await _persistence.GetById<Specialty>(id)
                ?? throw new EntityNotFoundException(nameof(Specialty));

            specialty.UpdateDetails(request.Name, request.Description);

            await _persistence.Update(specialty);

            return MapResponse(specialty);
        }

        public async Task DeleteSpecialty(Guid id)
        {
           var specialty = await _persistence.GetById<Specialty>(id)
               ?? throw new EntityNotFoundException(nameof(Specialty));

           specialty.SetDeleted();

           await _persistence.Update(specialty);
        }

        #region Private Methods
        private static SpecialtyModel.Response MapResponse(Specialty s)
        {
            return new SpecialtyModel.Response(
                s.Id,
                s.Name,
                s.Description);
        }
        #endregion
    }
}
