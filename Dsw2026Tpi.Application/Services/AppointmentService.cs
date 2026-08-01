using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Enums;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Dsw2026Tpi.Application.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly IPersistence _persistence;
        private readonly ILogger<AppointmentService> _logger;

        public AppointmentService(IPersistence persistence, ILogger <AppointmentService> logger)
        {
            _persistence = persistence;
            _logger = logger;
        }

        public async Task<AppointmentModel.Response> CreateAppointment(AppointmentModel.Request request)
        {
            ValidateAppointmentRequest(request);

            var patientRequest = request.Patient!;
            var reason = request.Reason!;

            var doctor = await ValidateDoctor(request.DoctorId);
            var patient = await ValidatePatient(patientRequest.Dni);
            var slot = await ValidateSlot(request.AvailabilitySlotId, doctor.Id);

            if (slot.Status != SlotStatus.AVAILABLE)
            {
                _logger.LogWarning("Intento de reserva sobre un turno no disponible. Slot: {SlotId}, Paciente: {PatientDni}",
                    slot.Id, patientRequest.Dni);
                throw new ConflictException(ErrorCodes.APPOINTMENT_CONFLICT, nameof(ErrorCodes.APPOINTMENT_CONFLICT))
                   .WithDetail(nameof(request.AvailabilitySlotId), "Turno no disponible.");
            }
            var slotDateTime = slot.SlotDate.Date.Add(slot.StartTime);

            if (slotDateTime < DateTime.Now)
                throw new ValidationException(ErrorCodes.VALIDATION_ERROR, nameof(ErrorCodes.VALIDATION_ERROR))
                    .WithDetail(nameof(request.AvailabilitySlotId), "No se pueden reservar turnos pasados.");

            slot.Book();
            await _persistence.Update(slot);

            var appointment = new Appointment(slot.Id, patient.Id, reason);
            var createdAppointment = await _persistence.Add(appointment);
            _logger.LogInformation("Turno reservado. Cita: {AppointmentId}, Paciente: {PatientDni}, Médico: {DoctorId}, Fecha: {SlotDate} {StartTime}",
                appointment.Id, patient.Dni, doctor.Id, slot.SlotDate.ToString("yyyy-MM-dd"), slot.StartTime);

            return MapAppointmentResponse(
                createdAppointment,
                doctor.Id,
                slot);
        }

        public async Task CancelAppointment(Guid id, string dniDelToken)
        {
            var appointment = await _persistence.GetById<Appointment>(id,  nameof(Appointment.AvailabilitySlot), nameof(Appointment.Patient))
                ?? throw new EntityNotFoundException(nameof(Appointment));

            if (appointment.Patient!.Dni != dniDelToken)
                throw new AuthorizationException();

            if (appointment.Status != AppointmentStatus.BOOKED)
                throw new ConflictException(ErrorCodes.INVALID_STATUS, nameof(ErrorCodes.INVALID_STATUS))
                    .WithDetail(nameof(appointment.Status),"El turno no está reservado.");

            appointment.Cancel();

            var slot = appointment.AvailabilitySlot!;
            slot.Free();
            slot.UpdatedAt = DateTime.Now;

            await _persistence.Update(appointment);
            _logger.LogInformation("Turno cancelado. Cita: {AppointmentId}, Slot liberado: {SlotId}",
                 appointment.Id, slot.Id);
        }

        public async Task<IEnumerable<SearchModel.Response>> GetDailyAppointments(DateOnly? date)
        {
            if (!date.HasValue)
                throw new ValidationException()
                    .WithDetail(nameof(date), "La fecha es obligatoria.");

            var appointments = await _persistence.GetFiltered<Appointment>(
                a =>
                    a.AvailabilitySlot!.SlotDate.Year == date.Value.Year &&
                    a.AvailabilitySlot.SlotDate.Month == date.Value.Month &&
                    a.AvailabilitySlot.SlotDate.Day == date.Value.Day,
            "AvailabilitySlot.AvailabilityRule.Doctor.Specialty","Patient");

            if (appointments is null)
                return [];

            return appointments
                .OrderBy(a => a.AvailabilitySlot!.SlotDate)
                .ThenBy(appointment => appointment.AvailabilitySlot!.StartTime)
                .Select(MapSearchResponse)
                .ToList();
        }

        public async Task<IEnumerable<AppointmentModel.Response>> GetPatientAppointments(long? dni)
        {
            if (!dni.HasValue)
                throw new ValidationException()
                    .WithDetail(nameof(dni), "El DNI es obligatorio.");
            if (!dni.Value.IsDniValid())
                throw new ValidationException()
                    .WithDetail(nameof(dni),"Debe indicar un DNI válido.");
  
            var dniString = dni.ToString();
            var today = DateTime.Today;

            var appointments = await _persistence.GetFiltered<Appointment>(a => a.Patient!.Dni == dniString && a.Status == AppointmentStatus.BOOKED && a.AvailabilitySlot!.SlotDate >= today,"AvailabilitySlot.AvailabilityRule");
            if (appointments is null)
                return [];

            return appointments 
                .OrderBy(appointment => appointment.AvailabilitySlot!.SlotDate)
                .ThenBy(appointment => appointment.AvailabilitySlot!.StartTime)
                .Select(appointment => MapAppointmentResponse( 
                    appointment, 
                    appointment.AvailabilitySlot!.AvailabilityRule!.DoctorId, 
                    appointment.AvailabilitySlot))
                .ToList();
        }

        public async Task<Pagination<SearchModel.Response>> SearchAppointments(SearchModel.Request request)
        {
            ValidateSearchRequest(request);

            var appointments = await _persistence.Paginate<Appointment, DateTime>(
                request.PageSize,
                request.PageIndex,
                a =>
                    (!request.SpecialtyId.HasValue || a.AvailabilitySlot!.AvailabilityRule!.Doctor!.SpecialtyId == request.SpecialtyId.Value) &&
                    (!request.DoctorId.HasValue || a.AvailabilitySlot!.AvailabilityRule!.DoctorId == request.DoctorId.Value) &&
                    (string.IsNullOrWhiteSpace(request.Dni) || a.Patient!.Dni.Contains(request.Dni)) &&

                    (!request.Date.HasValue ||
                        (a.AvailabilitySlot!.SlotDate.Year == request.Date.Value.Year &&
                         a.AvailabilitySlot.SlotDate.Month == request.Date.Value.Month &&
                         a.AvailabilitySlot.SlotDate.Day == request.Date.Value.Day)),

                a => a.AvailabilitySlot!.SlotDate,
                "AvailabilitySlot.AvailabilityRule.Doctor.Specialty","Patient");

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

            if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length < 5)
                throw new ValidationException()
                    .WithDetail(nameof(request.Reason),"El motivo debe tener al menos 5 caracteres.");
        }

        private async Task<Doctor> ValidateDoctor(Guid doctorId)
        {
            if (doctorId == Guid.Empty)
                throw new ValidationException()
                    .WithDetail(nameof(doctorId),"Debe indicar un DoctorId válido.");

            return await _persistence.GetById<Doctor>(doctorId,nameof(Doctor.Specialty))
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

            var slot = await _persistence.GetById<AvailabilitySlot>(availabilityId,
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

        private static AppointmentModel.Response MapAppointmentResponse(Appointment appointment, Guid doctorId, AvailabilitySlot slot)
        {
            return new AppointmentModel.Response(
                appointment.Id,
                doctorId,
                slot.Id,
                appointment.PatientId,
                DateOnly.FromDateTime(slot.SlotDate),
                slot.StartTime.ToString(@"hh\:mm"),
                slot.EndTime.ToString(@"hh\:mm"),
                appointment.Reason,
                appointment.Status.ToString());
        }

        private static SearchModel.Response MapSearchResponse(Appointment appointment)
        {
            var slot = appointment.AvailabilitySlot!;
            var doctor = slot.AvailabilityRule!.Doctor!;

            return new SearchModel.Response(
                appointment.Id,
                doctor.Specialty!.Name,
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