using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Dsw2026Tpi.Application.Services
{
    public class AvailabilityService : IAvailabilityService
    {
        private readonly IPersistence _persistence;
        private readonly IHolidayProvider _holidayProvider;
        private readonly ILogger<AvailabilityService> _logger;
        public AvailabilityService(IPersistence persistence, IHolidayProvider holidayProvider, ILogger<AvailabilityService> logger)
        {
            _persistence = persistence;
            _holidayProvider = holidayProvider;
            _logger = logger;
        }

        public async Task <IEnumerable<AvailabilityModel.Response>> CreateAvailability (AvailabilityModel.Request request)
        {
            var doctor = await ValidateDoctor(request.DoctorId);

            var today = DateTime.Today;
            short year = (short)today.Year;
            byte month = (byte)today.Month;

            var holidays = _holidayProvider.GetHolidays(year, month);

            ValidateDays(request.Days);

            var existingRules = await _persistence.GetFiltered<AvailabilityRule>
                (r => r.DoctorId == request.DoctorId && r.Month == month && r.Year == year && r.Deleted == false);
           
            if (existingRules != null)
                CheckOverlaps(request.Days, existingRules);

            var responses = new List<AvailabilityModel.Response>();

            foreach (var dayReq in request.Days)
            {
                var dayOfWeek = DayOfWeekConverter.Parse(dayReq.Day);
                var rule = new AvailabilityRule(request.DoctorId, month, year, (byte)dayOfWeek, dayReq.StartTime, dayReq.EndTime);
                rule.GenerateSlotsForRestOfMonth(today, holidays);

                if (rule.Slots.Any())
                {
                    var createdRule = await _persistence.Add(rule);

                    responses.Add(MapResponse(createdRule));
                }     
            }

            _logger.LogInformation("Disponibilidad configurada para el médico {DoctorId}. Período: {Month}/{Year}, Días: {Days}",
               request.DoctorId, month, year, request.Days.Count);

            return responses;
        }

        public async Task <IEnumerable<AvailabilityModel.Response>> UpdateAvailability(AvailabilityModel.Request request)
        {
            var doctor = await ValidateDoctor(request.DoctorId);

            var today = DateTime.Today;
            short year = (short)today.Year;
            byte month = (byte)today.Month;

            var holidays = _holidayProvider.GetHolidays(year, month);

            ValidateDays(request.Days);

            var existingRules = await _persistence.GetFiltered<AvailabilityRule>
                (r => r.DoctorId == request.DoctorId && r.Month == month && r.Year == year && r.Deleted == false, "Slots");

            if (existingRules != null)
            {
                foreach (var rule in existingRules)
                {
                    rule.SetDeleted();
                    await _persistence.Update(rule);

                    if (rule.Slots != null)
                    {
                        foreach (var slot in rule.Slots)
                        {
                            slot.SetDeleted();
                            await _persistence.Update(slot);
                        }
                    }
                }
            }
            CheckOverlaps(request.Days, new List<AvailabilityRule>());

            var responses = new List<AvailabilityModel.Response>();
            foreach (var dayReq in request.Days)
            {
                var dayOfWeek = DayOfWeekConverter.Parse(dayReq.Day);
                var rule = new AvailabilityRule(request.DoctorId, month, year, (byte)dayOfWeek, dayReq.StartTime, dayReq.EndTime);

                rule.GenerateSlotsForRestOfMonth(today, holidays);

                if (rule.Slots.Any())
                {
                    var createdRule = await _persistence.Add(rule);

                    responses.Add(MapResponse(createdRule));
                } 
            }
            _logger.LogInformation("Disponibilidad actualizada para el médico {DoctorId}. Período: {Month}/{Year}, Días: {Days}",
                request.DoctorId, month, year, request.Days.Count);
            return responses;
        }

        #region Private Methods
        private void ValidateDays(List<AvailabilityModel.DayRequest> days)
        {
            if (days == null || days.Count == 0) 
                throw new ValidationException()
                    .WithDetail(nameof(days), "Debe enviar al menos un dia.");

            foreach (var day in days)
            {
                if (day.StartTime >= day.EndTime) 
                    throw new ValidationException()
                        .WithDetail(nameof(day.StartTime), "La hora de inicio debe ser menor que la de fin.");
                
                if ((day.EndTime - day.StartTime).TotalMinutes < 30) 
                    throw new ValidationException()
                        .WithDetail(nameof(day.EndTime), "El intervalo debe ser de al menos 30 minutos.");
            }

            var grouped = days.GroupBy(d => d.Day.ToUpper());

            foreach (var group in grouped)
            {
                var ordered = group.OrderBy(g => g.StartTime).ToList();
                for (int i = 0; i < ordered.Count - 1; i++)
                {
                    if (ordered[i].EndTime > ordered[i + 1].StartTime)
                        throw new ConflictException(ErrorCodes.AVAILABILITY_CONFLICT, nameof(ErrorCodes.AVAILABILITY_CONFLICT))
                            .WithDetail(nameof(AvailabilityModel.DayRequest.Day), $"Se detectó un solapamiento de horarios para el día {ordered[i].Day}.");
                }
            }
        }

        private void CheckOverlaps(List<AvailabilityModel.DayRequest> requests, IEnumerable<AvailabilityRule> existingRules)
        {
            foreach (var req in requests)
            {
                var parsedDay = (byte)DayOfWeekConverter.Parse(req.Day);
                var ruleOverlaps = existingRules.Where(r => r.DayOfWeek == parsedDay);

                foreach (var rule in ruleOverlaps)
                {
                    if (req.StartTime < rule.EndTime && req.EndTime > rule.StartTime)
                        throw new ConflictException(ErrorCodes.AVAILABILITY_CONFLICT, nameof(ErrorCodes.AVAILABILITY_CONFLICT))
                            .WithDetail(nameof(req.Day), $"Ya existe una disponibilidad para el día {req.Day} en el horario solicitado.");
                }
            }
        }

        private async Task <Doctor> ValidateDoctor (Guid doctorId)
        {
            if (doctorId == Guid.Empty )
                throw new ValidationException() 
                    .WithDetail(nameof(doctorId), "Debe indicar un DoctorId válido.");
            
            return await _persistence.GetById<Doctor>(doctorId)
                ?? throw new EntityNotFoundException(nameof(Doctor));
        }

        private static AvailabilityModel.Response MapResponse(AvailabilityRule rule)
        {
            return new AvailabilityModel.Response(
                rule.Id,
                DayOfWeekConverter.GetDayName(rule.DayOfWeek),
                rule.StartTime.ToString(@"hh\:mm"),
                rule.EndTime.ToString(@"hh\:mm"));
        }
        #endregion
    }
}
