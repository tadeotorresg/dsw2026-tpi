using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
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
            d => d.Name, nameof(Doctor.Specialty));

        return doctors.Map(MapResponse);
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
            .OrderBy(rule => rule.DayOfWeek == 0 ? 7 : rule.DayOfWeek)
            .Select(MapAvailabilityResponse)
            .ToList();
    }
    public async Task<DoctorModel.Response> CreateDoctor (DoctorModel.Request request)
    {
        if (!request.Name.IsNameValid())
            throw new ValidationException()
                .WithDetail(nameof(request.Name), "El nombre debe tener entre 3 y 100 caracteres.");

        if (!request.LicenseNumber.IsLicenseNumberValid())
            throw new ValidationException()
                .WithDetail(nameof(request.LicenseNumber),"La matrícula no puede estar vacía.");     
        
        var specialty = await _persistence.GetById<Specialty>(request.SpecialtyId)
            ?? throw new EntityNotFoundException(nameof(Specialty));

        var doctor = new Doctor(request.Name, request.LicenseNumber, specialty);

        await _persistence.Add(doctor);

        return MapResponse(doctor);      
    }

    public async Task<DoctorModel.Response> UpdateDoctor (Guid id, DoctorModel.Request request)
    {
        if (!request.Name.IsNameValid())
            throw new ValidationException()
                .WithDetail(nameof(request.Name), "El nombre debe tener entre 3 y 100 caracteres.");

        if (!request.LicenseNumber.IsLicenseNumberValid())
            throw new ValidationException()
                .WithDetail(nameof(request.LicenseNumber),"La matrícula no puede estar vacía.");
        
        var specialty = await _persistence.GetById<Specialty>(request.SpecialtyId)
            ?? throw new EntityNotFoundException(nameof(Specialty));

        var doctor = await _persistence.GetById<Doctor>(id)
            ?? throw new EntityNotFoundException(nameof(Doctor));

        doctor.UpdateProfile(request.Name, request.LicenseNumber, specialty);

        await _persistence.Update(doctor);

        return MapResponse(doctor);
    }

    public async Task DeleteDoctor(Guid id)
    {
        var doctor = await _persistence.GetById<Doctor>(id)
            ?? throw new EntityNotFoundException(nameof(Doctor));

        doctor.SetDeleted();

        await _persistence.Update(doctor);
    }

    #region Private Methods
    private static DoctorModel.Response MapResponse(Doctor d)
    {
        return new DoctorModel.Response(
            d.Id,
            d.Name,
            d.LicenseNumber,
            new DoctorModel.SpecialtyDto(d.Specialty?.Id, d.Specialty?.Name));
    }

    private static AvailabilityModel.Response MapAvailabilityResponse(AvailabilityRule rule)
    {
        return new AvailabilityModel.Response(
            rule.DoctorId,
            DayOfWeekConverter.GetDayName(rule.DayOfWeek),
            rule.StartTime.ToString(@"hh\:mm"),
            rule.EndTime.ToString(@"hh\:mm"));
    }
    #endregion
}
