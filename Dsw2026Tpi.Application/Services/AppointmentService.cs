using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Enums;
using Dsw2026Tpi.Domain.Interfaces;
using Dsw2026Tpi.CrossCutting.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IPersistence _persistence;

        public AppointmentService(IPersistence persistence)
        {
            _persistence = persistence;
        }

        public async Task<AppointmentModel.Response> CreateAppointment(AppointmentModel.Request request)
        {
            ValidateAppointmentRequest(request);

            var doctor = await ValidateDoctor(request.DoctorId);
            var patient = await ValidatePatient(request.Patient.Dni);
            var slot = await ValidateSlot(request.AvailabilityId, doctor.Id);

            if (slot.Status != SlotStatus.AVAILABLE)
                throw new ConflictException(ErrorCodes.AVAILABILITY_CONFLICT, ErrorCodes.AVAILABILITY_CONFLICT)
                    .WithDetail("dateTime", "slot_unavailable");

            var slotDateTime = slot.SlotDate.Date.Add(slot.StartTime);

            if (slotDateTime < DateTime.Now)
                throw new ValidationException(ErrorCodes.VALIDATION_ERROR, ErrorCodes.VALIDATION_ERROR)
                    .WithDetail(nameof(request.AvailabilityId), "No se pueden reservar turnos pasados.");

            slot.Book();
            slot.UpdatedAt = DateTime.UtcNow;
            await _persistence.Update(slot);

            var appointment = new Appointment(slot.Id, patient.Id, request.Reason);
            await _persistence.Add(appointment);

            return new AppointmentModel.Response(
                appointment.Id,
                appointment.Status.ToString(),
                appointment.Reason,
                DateOnly.FromDateTime(slot.SlotDate),
                slot.StartTime.ToString(@"hh\:mm"),
                slot.EndTime.ToString(@"hh\:mm"),
                new AppointmentModel.DoctorDto(doctor.Id, doctor.Name,
                new AppointmentModel.SpecialityDto(doctor.Speciality!.Id, doctor.Speciality.Name)),
                new AppointmentModel.PatientDto(patient.Id, patient.Dni));
        }

        public async Task CancelAppointment(Guid id)
        {
            var appointment = await _persistence.GetById<Appointment>(id,  nameof(Appointment.AvailabilitySlot))
                ?? throw new EntityNotFoundException(nameof(Appointment));

            if (appointment.Status != AppointmentStatus.BOOKED)
                throw new ConflictException(ErrorCodes.INVALID_STATUS, nameof(ErrorCodes.INVALID_STATUS))
                    .WithDetail(nameof(appointment.Status),"El turno no está reservado");

            appointment.Cancel();
            appointment.UpdatedAt = DateTime.UtcNow;

            var slot = appointment.AvailabilitySlot!;
            slot.Free();
            slot.UpdatedAt = DateTime.UtcNow;

            await _persistence.Update(appointment);
        }

      
        public async Task<IEnumerable<AppointmentModel.SearchResponse>> GetDailyAppointments(DateOnly date)
        {
            var appointments = await _persistence.GetFiltered<Appointment>(
                a =>
                    a.AvailabilitySlot!.SlotDate.Year == date.Year &&
                    a.AvailabilitySlot.SlotDate.Month == date.Month &&
                    a.AvailabilitySlot.SlotDate.Day == date.Day,
                "AvailabilitySlot.AvailabilityRule.Doctor.Speciality");

            if (appointments is null)
                return [];

            return appointments
                .OrderBy(a => a.AvailabilitySlot!.StartTime)
                .Select(a => new AppointmentModel.SearchResponse(
                    a.AvailabilitySlot!
                        .AvailabilityRule!
                        .Doctor!
                        .Speciality!
                        .Name,

                    a.AvailabilitySlot
                        .AvailabilityRule
                        .Doctor
                        .Name,

                    $"{a.AvailabilitySlot.SlotDate:yyyy-MM-dd} " +
                    $"{a.AvailabilitySlot.StartTime:hh\\:mm} - " +
                    $"{a.AvailabilitySlot.EndTime:hh\\:mm}"
                ))
                .ToList();
        }
        

        public async Task<IEnumerable<AppointmentModel.PatientAppointmentResponse>> GetPatientAppointments(long dni)
        {
            if (!dni.IsDniValid())
            throw new ValidationException()
                    .WithDetail(nameof(dni),"Debe indicar un DNI válido.");
  
            var dniString = dni.ToString();

            var appointments = await _persistence.GetFiltered<Appointment>(a => a.Patient!.Dni == dniString && a.Status == AppointmentStatus.BOOKED && !a.Deleted,
                                                                           "AvailabilitySlot.AvailabilityRule.Doctor.Speciality","Patient");

            if (appointments is null)
                return [];

            return appointments
                .Select(a => new AppointmentModel.PatientAppointmentResponse(
                    a.Id,
                    DateOnly.FromDateTime(a.AvailabilitySlot!.SlotDate),
                    a.AvailabilitySlot.StartTime.ToString(@"hh\:mm"),
                    a.AvailabilitySlot.EndTime.ToString(@"hh\:mm"),
                    a.Status.ToString(),
                    a.Reason,
                    new AppointmentModel.DoctorDto(
                        a.AvailabilitySlot.AvailabilityRule!.Doctor!.Id,
                        a.AvailabilitySlot.AvailabilityRule.Doctor.Name,
                        new AppointmentModel.SpecialityDto(
                            a.AvailabilitySlot.AvailabilityRule.Doctor.Speciality!.Id,
                            a.AvailabilitySlot.AvailabilityRule.Doctor.Speciality.Name))))
                .ToList();
        }


        public async Task<Pagination<AppointmentModel.SearchResponse>> SearchAppointments(Guid? specialtyId, Guid? doctorId, string? dni, DateOnly? date, int pageSize, int pageIndex)
        {
            var appointments = await _persistence.Paginate<Appointment, DateTime>(
                pageSize,
                pageIndex,
                a =>
                    (!specialtyId.HasValue || a.AvailabilitySlot!.AvailabilityRule!.Doctor!.SpecialityId == specialtyId.Value) &&
                    (!doctorId.HasValue || a.AvailabilitySlot!.AvailabilityRule!.DoctorId == doctorId.Value) &&
                    (string.IsNullOrWhiteSpace(dni) || a.Patient!.Dni.Contains(dni)) &&

                    (!date.HasValue ||
                        (a.AvailabilitySlot!.SlotDate.Year == date.Value.Year &&
                         a.AvailabilitySlot.SlotDate.Month == date.Value.Month &&
                         a.AvailabilitySlot.SlotDate.Day == date.Value.Day)),

                a => a.AvailabilitySlot!.SlotDate,
                "AvailabilitySlot.AvailabilityRule.Doctor.Speciality",
                "Patient");

            return appointments.Map(a => new AppointmentModel.SearchResponse(
                a.AvailabilitySlot!.AvailabilityRule!.Doctor!.Speciality!.Name,
                a.AvailabilitySlot.AvailabilityRule.Doctor.Name,
                $"{a.AvailabilitySlot.SlotDate:yyyy-MM-dd} {a.AvailabilitySlot.StartTime:hh\\:mm} - {a.AvailabilitySlot.EndTime:hh\\:mm}"
            ));
        }

        #region Private Methods

        private void ValidateAppointmentRequest(AppointmentModel.Request request)
        {
            if (request.Patient is null)
                throw new ValidationException()
                    .WithDetail(
                        nameof(request.Patient),
                        "Debe indicar los datos del paciente.");

            if (!request.Patient.Dni.IsDniValid())
                throw new ValidationException()
                    .WithDetail(
                        nameof(request.Patient.Dni),
                        "Debe indicar un DNI válido de entre 7 y 8 dígitos.");

            if (string.IsNullOrWhiteSpace(request.Reason) ||
                request.Reason.Length < 5)
                throw new ValidationException()
                    .WithDetail(
                        nameof(request.Reason),
                        "El motivo debe tener al menos 5 caracteres.");
        }

        private async Task<Doctor> ValidateDoctor(Guid doctorId)
        {
            if (doctorId == Guid.Empty)
                throw new ValidationException()
                    .WithDetail(
                        nameof(doctorId),
                        "Debe indicar un DoctorId válido.");

            return await _persistence.GetById<Doctor>(
                doctorId,
                nameof(Doctor.Speciality))
                ?? throw new EntityNotFoundException(nameof(Doctor));
        }

        private async Task<Patient> ValidatePatient(long dni)
        {
            return await _persistence.First<Patient>(
                patient => patient.Dni == dni.ToString())
                ?? throw new EntityNotFoundException(nameof(Patient));
        }

        private async Task<AvailabilitySlot> ValidateSlot(
            Guid availabilityId,
            Guid doctorId)
        {
            if (availabilityId == Guid.Empty)
                throw new ValidationException()
                    .WithDetail(
                        nameof(availabilityId),
                        "Debe indicar un AvailabilityId válido.");

            var slot = await _persistence.GetById<AvailabilitySlot>(
                availabilityId,
                nameof(AvailabilitySlot.AvailabilityRule))
                ?? throw new EntityNotFoundException(nameof(AvailabilitySlot));

            if (slot.AvailabilityRule!.DoctorId != doctorId)
                throw new ValidationException()
                    .WithDetail(
                        nameof(availabilityId),
                        "El turno no pertenece al doctor indicado.");

            return slot;
        }

        #endregion
    }


}
 
