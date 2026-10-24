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

        public Task <IEnumerable<AvailabilityModel.Response>> CreateAvailability (AvailabilityModel.Request request)
        {
            return SaveAvailability(request, replace: false);
        }

        public Task <IEnumerable<AvailabilityModel.Response>> UpdateAvailability(AvailabilityModel.Request request)
        {
            return SaveAvailability(request, replace: true);
        }

        private async Task<IEnumerable<AvailabilityModel.Response>> SaveAvailability(AvailabilityModel.Request request, bool replace)
        {
            ValidateDays(request.Days);

            var requestedRules = request.Days
                .Select(day => new
                {
                    DayOfWeek = (byte)DayOfWeekConverter.Parse(day.Day),
                    day.StartTime,
                    day.EndTime
                })
                .ToList();

            var responses = await _persistence.ExecuteInTransactionAsync<IEnumerable<AvailabilityModel.Response>>(
                async () =>
                    {
                        await ValidateDoctor(request.DoctorId);

                        var today = DateTime.Today;
                        short year = (short)today.Year;
                        byte month = (byte)today.Month;

                        var holidays = _holidayProvider.GetHolidays(year, month);

                        var existingRules =
                            (await _persistence.GetFiltered<AvailabilityRule>(
                                rule =>
                                    rule.DoctorId == request.DoctorId &&
                                    rule.Year == year &&
                                    rule.Month == month &&
                                    !rule.Deleted) ?? [])
                            .ToList();

                        if (replace && existingRules.Count == 0)
                            throw new EntityNotFoundException(
                                nameof(AvailabilityRule));

                        if (!replace)
                            CheckOverlaps(request.Days, existingRules);

                        if (replace)
                        {
                            foreach (var rule in existingRules)
                            {
                                var remainsRequested = requestedRules.Any(
                                    requested =>
                                        requested.DayOfWeek == rule.DayOfWeek &&
                                        requested.StartTime == rule.StartTime &&
                                        requested.EndTime == rule.EndTime);

                                if (remainsRequested)continue;

                                rule.SetDeleted();

                                await _persistence.Update(rule);
                            }
                        }

                        var responseRules = new List<AvailabilityRule>();

                        foreach (var requested in requestedRules)
                        {
                            var rule = existingRules.FirstOrDefault(
                                existing =>
                                    !existing.Deleted &&
                                    existing.DayOfWeek == requested.DayOfWeek &&
                                    existing.StartTime == requested.StartTime &&
                                    existing.EndTime == requested.EndTime);

                            if (rule is null)
                            {
                                rule = new AvailabilityRule(
                                    request.DoctorId,
                                    month,
                                    year,
                                    requested.DayOfWeek,
                                    requested.StartTime,
                                    requested.EndTime);

                                rule.GenerateSlotsForRestOfMonth(today, holidays);

                                await _persistence.Add(rule);

                                existingRules.Add(rule);
                            }

                            responseRules.Add(rule);
                        }

                        return responseRules
                            .Select(MapResponse)
                            .ToList();
                    },
                    request.DoctorId);

            _logger.LogInformation("Disponibilidad guardada para el médico {DoctorId}. Días: {Days}", request.DoctorId, request.Days.Count);

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
                DayOfWeekConverter.Parse(day.Day);

                if (day.StartTime >= day.EndTime) 
                    throw new ValidationException()
                        .WithDetail(nameof(day.StartTime), "La hora de inicio debe ser menor que la de fin.");
                
                if ((day.EndTime - day.StartTime).TotalMinutes < 30) 
                    throw new ValidationException()
                        .WithDetail(nameof(day.EndTime), "El intervalo debe ser de al menos 30 minutos.");
            }

            var grouped = days.GroupBy(d => DayOfWeekConverter.Parse(d.Day));

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
