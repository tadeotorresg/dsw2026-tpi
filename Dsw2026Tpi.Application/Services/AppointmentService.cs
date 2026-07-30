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

            var patientRequest = request.Patient!;
            var reason = request.Reason!;

            var doctor = await ValidateDoctor(request.DoctorId);
            var patient = await ValidatePatient(patientRequest.Dni);
            var slot = await ValidateSlot(request.AvailabilityId, doctor.Id);

            if (slot.Status != SlotStatus.AVAILABLE)
                throw new ConflictException(ErrorCodes.APPOINTMENT_CONFLICT, nameof(ErrorCodes.APPOINTMENT_CONFLICT))
                    .WithDetail(nameof(request.AvailabilityId), "Turno no disponible");

            var slotDateTime = slot.SlotDate.Date.Add(slot.StartTime);

            if (slotDateTime < DateTime.Now)
                throw new ValidationException(ErrorCodes.VALIDATION_ERROR, nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(nameof(request.AvailabilityId), "No se pueden reservar turnos pasados.");

            slot.Book();
            slot.UpdatedAt = DateTime.Now;
            await _persistence.Update(slot);

            var appointment = new Appointment(slot.Id, patient.Id, reason);
            var createdAppointment = await _persistence.Add(appointment);

                return new AppointmentModel.Response(
                createdAppointment.Id,
                doctor.Id,
                slot.Id,
                patient.Id,
                DateOnly.FromDateTime(slot.SlotDate),
                slot.StartTime.ToString(@"hh\:mm"),
                slot.EndTime.ToString(@"hh\:mm"),
                createdAppointment.Reason,
                createdAppointment.Status.ToString());
        }

        public async Task CancelAppointment(Guid id)
        {
            var appointment = await _persistence.GetById<Appointment>(id,  nameof(Appointment.AvailabilitySlot))
                ?? throw new EntityNotFoundException(nameof(Appointment));

            if (appointment.Status != AppointmentStatus.BOOKED)
                throw new ConflictException(ErrorCodes.INVALID_STATUS, nameof(ErrorCodes.INVALID_STATUS))
                    .WithDetail(nameof(appointment.Status),"El turno no está reservado");

            appointment.Cancel();
            appointment.UpdatedAt = DateTime.Now;

            var slot = appointment.AvailabilitySlot!;
            slot.Free();
            slot.UpdatedAt = DateTime.Now;

            await _persistence.Update(appointment);
        }

        public async Task<IEnumerable<SearchModel.Response>> GetDailyAppointments(DateOnly? date)
        {
            if (!date.HasValue)
                return [];
            var appointments = await _persistence.GetFiltered<Appointment>(
                a =>
                    a.AvailabilitySlot!.SlotDate.Year == date.Value.Year &&
                    a.AvailabilitySlot.SlotDate.Month == date.Value.Month &&
                    a.AvailabilitySlot.SlotDate.Day == date.Value.Day,
            "AvailabilitySlot.AvailabilityRule.Doctor.Speciality","Patient");

            if (appointments is null)
                return [];

            return appointments
                .OrderBy(a => a.AvailabilitySlot!.SlotDate)
                .ThenBy(appointment => appointment.AvailabilitySlot!.StartTime)
                .Select(MapSearchResponse)
                .ToList();
        }

        public async Task<IEnumerable<AppointmentModel.Response>> GetPatientAppointments(long dni)
        {
            if (!dni.IsDniValid())
            throw new ValidationException()
                    .WithDetail(nameof(dni),"Debe indicar un DNI válido.");
  
            var dniString = dni.ToString();
            var today = DateTime.Today;

            var appointments = await _persistence.GetFiltered<Appointment>(a => a.Patient!.Dni == dniString && a.Status == AppointmentStatus.BOOKED && a.AvailabilitySlot!.SlotDate >= today,
                                                                           "AvailabilitySlot.AvailabilityRule");
            if (appointments is null)
                return [];

            return appointments 
                .OrderBy(appointment => appointment.AvailabilitySlot!.SlotDate)
                .ThenBy(appointment => appointment.AvailabilitySlot!.StartTime)
                .Select(appointment => new AppointmentModel.Response( 
                    appointment.Id, 
                    appointment.AvailabilitySlot!.AvailabilityRule!.DoctorId, 
                    appointment.AvailabilitySlotId, 
                    appointment.PatientId,DateOnly.FromDateTime(appointment.AvailabilitySlot.SlotDate),
                    appointment.AvailabilitySlot.StartTime.ToString(@"hh\:mm"),
                    appointment.AvailabilitySlot.EndTime.ToString(@"hh\:mm"),
                    appointment.Reason,
                    appointment.Status.ToString()))
                .ToList();
        }

        public async Task<Pagination<SearchModel.Response>> SearchAppointments(SearchModel.Request request)
        {
            ValidateSearchRequest(request);

            var appointments = await _persistence.Paginate<Appointment, DateTime>(
                request.PageSize,
                request.PageIndex,
                a =>
                    (!request.SpecialtyId.HasValue || a.AvailabilitySlot!.AvailabilityRule!.Doctor!.SpecialityId == request.SpecialtyId.Value) &&
                    (!request.DoctorId.HasValue || a.AvailabilitySlot!.AvailabilityRule!.DoctorId == request.DoctorId.Value) &&
                    (string.IsNullOrWhiteSpace(request.Dni) || a.Patient!.Dni.Contains(request.Dni)) &&

                    (!request.Date.HasValue ||
                        (a.AvailabilitySlot!.SlotDate.Year == request.Date.Value.Year &&
                         a.AvailabilitySlot.SlotDate.Month == request.Date.Value.Month &&
                         a.AvailabilitySlot.SlotDate.Day == request.Date.Value.Day)),

                a => a.AvailabilitySlot!.SlotDate,
                "AvailabilitySlot.AvailabilityRule.Doctor.Speciality","Patient");

            return appointments.Map(MapSearchResponse);
        }

        #region Private Methods
        private void ValidateAppointmentRequest(AppointmentModel.Request request)
        {
            if (request.Patient is null)
                throw new ValidationException()
                    .WithDetail(nameof(request.Patient),"Debe indicar los datos del paciente.");

            if (!request.Patient.Dni.IsDniValid())
                throw new ValidationException()
                    .WithDetail(nameof(request.Patient.Dni),"Debe indicar un DNI válido de entre 7 y 8 dígitos.");

            if (string.IsNullOrWhiteSpace(request.Reason) ||
                request.Reason.Length < 5)
                throw new ValidationException()
                    .WithDetail(nameof(request.Reason),"El motivo debe tener al menos 5 caracteres.");
        }

        private async Task<Doctor> ValidateDoctor(Guid doctorId)
        {
            if (doctorId == Guid.Empty)
                throw new ValidationException()
                    .WithDetail(nameof(doctorId),"Debe indicar un DoctorId válido.");

            return await _persistence.GetById<Doctor>(doctorId,nameof(Doctor.Speciality))
                ?? throw new EntityNotFoundException(nameof(Doctor));
        }

        private async Task<Patient> ValidatePatient(long dni)
        {
            return await _persistence.First<Patient>(patient => patient.Dni == dni.ToString())
                ?? throw new EntityNotFoundException(nameof(Patient));
        }

        private async Task<AvailabilitySlot> ValidateSlot(
            Guid availabilityId,
            Guid doctorId)
        {
            if (availabilityId == Guid.Empty)
                throw new ValidationException()
                    .WithDetail(nameof(availabilityId),"Debe indicar un AvailabilityId válido.");

            var slot = await _persistence.GetById<AvailabilitySlot>(
                availabilityId,
                nameof(AvailabilitySlot.AvailabilityRule))
                ?? throw new EntityNotFoundException(nameof(AvailabilitySlot));

            if (slot.AvailabilityRule!.DoctorId != doctorId)
                throw new ValidationException()
                    .WithDetail(nameof(availabilityId),"El turno no pertenece al doctor indicado.");

            return slot;
        }

        private void ValidateSearchRequest(SearchModel.Request request)
        {
            if (request.SpecialtyId.HasValue && request.SpecialtyId.Value == Guid.Empty)
                throw new ValidationException()
                    .WithDetail(nameof(request.SpecialtyId),"Debe indicar un identificador de especialidad válido.");

            if (request.DoctorId.HasValue && request.DoctorId.Value == Guid.Empty)
                throw new ValidationException()
                    .WithDetail(nameof(request.DoctorId),"Debe indicar un identificador de médico válido.");
        }

        private static SearchModel.Response MapSearchResponse(Appointment appointment)
        {
            var slot = appointment.AvailabilitySlot!;
            var doctor = slot.AvailabilityRule!.Doctor!;

            return new SearchModel.Response(
                appointment.Id,
                doctor.Speciality!.Name,
                doctor.Name,
                appointment.Patient!.Dni,
                DateOnly.FromDateTime(slot.SlotDate),
                slot.StartTime.ToString(@"hh\:mm"),
                slot.EndTime.ToString(@"hh\:mm"),
                appointment.Status.ToString());
        }
        #endregion
    }
}