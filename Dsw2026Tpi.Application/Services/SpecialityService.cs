using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Dsw2026Tpi.CrossCutting.Helpers;

namespace Dsw2026Tpi.Application.Services
{
    public class SpecialityService : ISpecialityService

    {
        private readonly IPersistence _persistence;

        public SpecialityService (IPersistence persistence)
        {
            _persistence = persistence;

        }

        public async Task<SpecialityModel.Response> CreateSpeciality(SpecialityModel.Request request)
        {
           
            if (!request.Name.IsNameValid())
                throw new ValidationException(ErrorCodes.VALIDATION_ERROR, nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(nameof(request.Name), "Nombre inválido, entre 3 y 100 caracteres.");

            if (!request.Description.IsDescriptionValid())
                throw new ValidationException(ErrorCodes.VALIDATION_ERROR, nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(nameof(request.Description), "Descripción inválida, entre 10 y 100 caracteres.");

            
            var speciality = new Speciality(request.Name, request.Description);

            await _persistence.Add(speciality);

            return new SpecialityModel.Response(
                speciality.Id,
                speciality.Name,
                speciality.Description
            );
        }

        public async Task<Pagination<SpecialityModel.Response>> GetAllSpeciality(int pageSize, int pageIndex, string? name = null)
        {
            var specialities = await _persistence.Paginate<Speciality, string>(
                pageSize,
                pageIndex,
                s => !s.Deleted &&
                (string.IsNullOrWhiteSpace(name) || s.Name.Contains(name)),
                x => x.Name
            );

            return specialities.Map(s => new SpecialityModel.Response(s.Id, s.Name, s.Description));
        }

        
        public async Task<SpecialityModel.Response> UpdateSpeciality(Guid id, SpecialityModel.Request request)
        {
            
            if (!request.Name.IsNameValid())
                throw new ValidationException(ErrorCodes.VALIDATION_ERROR, nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(nameof(request.Name), "El nombre es inválido. El campo debe tener entre 3 y 100 caracteres.");

            if (!request.Description.IsDescriptionValid())
                throw new ValidationException(ErrorCodes.VALIDATION_ERROR, nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(nameof(request.Description), "La descripción es inválida. El campo debe tener entre 10 y 100 caracteres.");

            
            var speciality = await _persistence.GetById<Speciality>(id)
                             ?? throw new EntityNotFoundException(nameof(Speciality));

            
            var specialityToUpdate = new Speciality(request.Name, request.Description, id);

            await _persistence.Update(specialityToUpdate);

            return new SpecialityModel.Response(
                specialityToUpdate.Id,
                specialityToUpdate.Name,
                specialityToUpdate.Description
            );
        }

        public async Task DeleteSpeciality(Guid id)
        {
            var speciality = await _persistence.GetById<Speciality>(id)
                             ?? throw new EntityNotFoundException(nameof(Speciality));

            await _persistence.Delete(speciality);
        }
    }
}
