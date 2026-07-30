using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

public class DoctorService : IDoctorService
{
    private readonly IPersistence _persistence;

    public DoctorService(IPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<Pagination<DoctorModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null)
    {
        if (!string.IsNullOrWhiteSpace(name) && !name.IsNameValid())
            throw new ValidationException()
                .WithDetail(nameof(name),"El nombre debe tener entre 3 y 100 caracteres.");

        var doctors = await _persistence.Paginate<Doctor, string>(pageSize, pageIndex, 
            d => d.IsActive && (string.IsNullOrWhiteSpace(name) ||d.Name.Contains(name)), 
            d => d.Name, nameof(Doctor.Speciality));

        return doctors.Map(d => new DoctorModel.Response(d.Id, d.Name, d.LicenseNumber,
            new DoctorModel.SpecialityDto(d.Speciality?.Id, d.Speciality?.Name)));
    }

    public async Task<IEnumerable<AvailabilityModel.Response>> GetAvailabilities(Guid doctorId)
    {
        if (doctorId == Guid.Empty) 
            throw new ValidationException()
                .WithDetail(nameof(doctorId), "Debe indicar un DoctorId válido.");
        
        var doctor = await _persistence.GetById<Doctor>(doctorId)
                 ?? throw new EntityNotFoundException(nameof(Doctor));

        var mes = (byte)DateTime.Today.Month;
        var año = (short)DateTime.Today.Year;

        var availabilityRules = await _persistence.GetFiltered<AvailabilityRule>(
            a => a.DoctorId == doctorId
            && a.Month == mes
            && a.Year == año);

        if (availabilityRules == null)
            return [];

        return availabilityRules
            .OrderBy(a => a.DayOfWeek == 0 ? 7 : a.DayOfWeek)
            .Select(a => new AvailabilityModel.Response(a.Id, DayOfWeekConverter.GetDayName(a.DayOfWeek),
                a.StartTime.ToString(@"hh\:mm"),
                a.EndTime.ToString(@"hh\:mm"))
            )
            .ToList();
    }
    public async Task<DoctorModel.Response> CreateDoctor (DoctorModel.Request request)
    {
        if (!request.Name.IsNameValid())
            throw new ValidationException()
                .WithDetail(nameof(request.Name), "El nombre debe tener entre 3 y 100 caracteres.");

        if (!request.LicenseNumber.IsLicenseNumberValid())
            throw new ValidationException()
                .WithDetail(nameof(request.LicenseNumber),"Debe indicar un número de matrícula.");     
        
        var speciality = await _persistence.GetById<Speciality>(request.SpecialityId)
            ?? throw new EntityNotFoundException(nameof(Speciality));

        var doctor = new Doctor(request.Name, request.LicenseNumber, speciality);

        await _persistence.Add(doctor);

        return new DoctorModel.Response( doctor.Id, doctor.Name, doctor.LicenseNumber,
            new DoctorModel.SpecialityDto(speciality.Id, speciality.Name));      
    }

    public async Task<DoctorModel.Response> UpdateDoctor (Guid id, DoctorModel.Request request)
    {
        if (!request.Name.IsNameValid())
            throw new ValidationException()
                .WithDetail(nameof(request.Name), "El nombre debe tener entre 3 y 100 caracteres.");

        if (string.IsNullOrWhiteSpace(request.LicenseNumber))
            throw new ValidationException()
                .WithDetail(nameof(request.LicenseNumber),"La matrícula es obligatoria.");
        
        var speciality = await _persistence.GetById<Speciality>(request.SpecialityId)
            ?? throw new EntityNotFoundException(nameof(Speciality));

        var doctor = await _persistence.GetById<Doctor>(id)
            ?? throw new EntityNotFoundException(nameof(Doctor));

        doctor.UpdateProfile(request.Name, request.LicenseNumber, speciality);
        doctor.UpdatedAt = DateTime.UtcNow;

        await _persistence.Update(doctor);

        return new DoctorModel.Response(doctor.Id, doctor.Name, doctor.LicenseNumber,
            new DoctorModel.SpecialityDto(speciality.Id, speciality.Name));
    }

    public async Task DeleteDoctor(Guid id)
    {
        var doctor = await _persistence.GetById<Doctor>(id)
            ?? throw new EntityNotFoundException(nameof(Doctor));

        doctor.SetDeleted();
        doctor.UpdatedAt = DateTime.UtcNow;

        await _persistence.Update(doctor);
    }
}
