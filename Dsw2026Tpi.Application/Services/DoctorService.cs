using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Enums;
using Dsw2026Tpi.Domain.Interfaces;
using System.Data;

namespace Dsw2026Tpi.Application.Services;

public class DoctorService : IDoctorService
{
    private readonly IPersistence _persistence;

    public DoctorService(IPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<Pagination<DoctorModel.Response>> GetAll(int pageSize, int pageIndex, string? name = null, Guid? specialtyId = null, bool onlyActive = true)
    {
        ValidationsExtensions.ValidatePagination(pageSize, pageIndex);

        if (!string.IsNullOrWhiteSpace(name) && !name.IsNameValid())
            throw new ValidationException()
                .WithDetail(nameof(name),"El nombre debe tener entre 3 y 100 caracteres.");

        if (specialtyId.HasValue && specialtyId.Value == Guid.Empty)
            throw new ValidationException()
                .WithDetail(
                    nameof(specialtyId),
                    "Debe indicar un identificador de especialidad válido.");

        var doctors = await _persistence.Paginate<Doctor, string>(pageSize, pageIndex,
            d =>!d.Deleted && (!onlyActive || (d.IsActive && d.Specialty != null && !d.Specialty.Deleted && d.Specialty.IsActive)) && (string.IsNullOrWhiteSpace(name) 
            || d.Name.Contains(name)) && (!specialtyId.HasValue || d.SpecialtyId == specialtyId.Value), 
            d => d.Name, nameof(Doctor.Specialty));

        return doctors.Map(MapResponse);
    }

    public async Task<IEnumerable<AvailabilityModel.Response>> GetAvailabilities(Guid doctorId)
    {
        if (doctorId == Guid.Empty) 
            throw new ValidationException()
                .WithDetail(nameof(doctorId), "Debe indicar un DoctorId válido.");

        await GetDoctor(doctorId);

        var mes = (byte)DateTime.Today.Month;
        var año = (short)DateTime.Today.Year;

        var availabilityRules = await _persistence.GetFiltered<AvailabilityRule>(
            a => a.DoctorId == doctorId
            && a.Month == mes
            && a.Year == año
            && !a.Deleted);

        if (availabilityRules == null)
            return [];

        return availabilityRules
            .OrderBy(rule => rule.DayOfWeek == 0 ? 7 : rule.DayOfWeek)
            .Select(MapAvailabilityResponse)
            .ToList();
    }
    public async Task<IEnumerable<AvailabilitySlotModel.Response>> GetAvailableSlots(Guid doctorId, DateOnly? fromDate = null, DateOnly? toDate = null)
    {
        if (doctorId == Guid.Empty)
            throw new ValidationException()
                .WithDetail(nameof(doctorId), "Debe indicar un DoctorId válido.");

        var doctor = await GetDoctor(doctorId);

        var now = DateTime.Now;
        var today = now.Date;
        var currentTime = now.TimeOfDay;

        var startDate = fromDate ?? DateOnly.FromDateTime(today);

        var endDate = toDate ?? new DateOnly(
            startDate.Year,
            startDate.Month,
            DateTime.DaysInMonth(startDate.Year, startDate.Month));

        if (startDate > endDate)
            throw new ValidationException()
                .WithDetail(
                    nameof(toDate),
                    "La fecha final debe ser mayor o igual que la fecha inicial.");

        if (!doctor.IsActive || doctor.Specialty is null || !doctor.Specialty.IsActive)
            return [];

        var startDateTime = startDate.ToDateTime(TimeOnly.MinValue);
        var endDateTime = endDate.ToDateTime(TimeOnly.MinValue);

        var slots = await _persistence.GetFiltered<AvailabilitySlot>(
            slot =>
                slot.AvailabilityRule!.DoctorId == doctorId &&
                !slot.AvailabilityRule.Deleted &&
                slot.Status == SlotStatus.AVAILABLE &&
                slot.SlotDate >= startDateTime &&
                slot.SlotDate <= endDateTime &&
                (slot.SlotDate > today ||
                    (slot.SlotDate == today &&
                     slot.StartTime >= currentTime)),
            nameof(AvailabilitySlot.AvailabilityRule));

        if (slots is null)
            return [];

        var occupiedSlots =(await _persistence.GetFiltered<AvailabilitySlot>(
            slot =>
                    slot.AvailabilityRule!.DoctorId == doctorId &&
                    slot.SlotDate >= startDateTime &&
                    slot.SlotDate <= endDateTime &&
                    (slot.Status == SlotStatus.BOOKED ||
                     slot.Status == SlotStatus.BLOCKED),
                nameof(AvailabilitySlot.AvailabilityRule)) ?? [])
            .ToList();

        return slots
            .Where(slot => !occupiedSlots.Any(occupied =>
                occupied.SlotDate == slot.SlotDate &&
                slot.StartTime < occupied.EndTime &&
                slot.EndTime > occupied.StartTime))
            .OrderBy(slot => slot.SlotDate)
            .ThenBy(slot => slot.StartTime)
            .Select(MapSlotResponse)
            .ToList();
    }

    public async Task<DoctorModel.Response> CreateDoctor (DoctorModel.Request request)
    {
        var specialty = await ValidateRequest(request);
        
        var doctor = new Doctor(request.Name, request.LicenseNumber, specialty);
        doctor.ChangeActiveStatus(request.IsActive ?? true);

        await _persistence.Add(doctor);

        return MapResponse(doctor);      
    }

    public async Task<DoctorModel.Response> UpdateDoctor (Guid id, DoctorModel.Request request)
    {
        return await _persistence.ExecuteInTransactionAsync<DoctorModel.Response>(async () =>
        {
            var specialty = await ValidateRequest(request);
            var doctor = await GetDoctor(id);

            doctor.UpdateProfile(request.Name, request.LicenseNumber, specialty);
            
            if (request.IsActive.HasValue)
                doctor.ChangeActiveStatus(request.IsActive.Value);

            await _persistence.Update(doctor);

            return MapResponse(doctor);
        },
        id);
    }

    public async Task DeleteDoctor(Guid id)
    {
        await _persistence.ExecuteInTransactionAsync<Guid>(async () =>
        {
            var doctor = await GetDoctor(id);
           
            doctor.SetDeleted();

            await _persistence.Update(doctor);

            return doctor.Id;
        },
        id);
    }

    #region Private Methods

    private async Task<Doctor> GetDoctor(Guid doctorId)
    {
        return await _persistence.First<Doctor>(d => d.Id == doctorId && !d.Deleted, nameof(Doctor.Specialty))
            ?? throw new EntityNotFoundException(nameof(Doctor));
    }
    private async Task<Specialty> ValidateRequest (DoctorModel.Request request)
    {
        if (!request.Name.IsNameValid())
            throw new ValidationException()
                .WithDetail(nameof(request.Name), "El nombre debe tener entre 3 y 100 caracteres.");

        if (!request.LicenseNumber.IsLicenseNumberValid())
            throw new ValidationException()
                .WithDetail(nameof(request.LicenseNumber), "La matrícula no puede estar vacía, ni superar los 50 caracteres.");
        
        return await _persistence.GetById<Specialty>(request.SpecialtyId)
            ?? throw new EntityNotFoundException(nameof(Specialty));
    }
    private static DoctorModel.Response MapResponse(Doctor d)
    {
        return new DoctorModel.Response(
            d.Id,
            d.Name,
            d.LicenseNumber,
            new DoctorModel.SpecialtyDto(d.Specialty?.Id, d.Specialty?.Name), d.IsActive);
    }

    private static AvailabilityModel.Response MapAvailabilityResponse(AvailabilityRule rule)
    {
        return new AvailabilityModel.Response(
            rule.Id,
            DayOfWeekConverter.GetDayName(rule.DayOfWeek),
            rule.StartTime.ToString(@"hh\:mm"),
            rule.EndTime.ToString(@"hh\:mm"));
    }
    private static AvailabilitySlotModel.Response MapSlotResponse(
    AvailabilitySlot slot)
    {
        return new AvailabilitySlotModel.Response(
            slot.Id,
            DateOnly.FromDateTime(slot.SlotDate),
            slot.StartTime.ToString(@"hh\:mm"),
            slot.EndTime.ToString(@"hh\:mm"));
    }
    #endregion
}
