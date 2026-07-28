using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using System.Text;

namespace Dsw2026Tpi.Application.Services
{
    public class AvailabilityService : IAvailabilityService
    {
        private readonly IPersistence _persistence;
        public AvailabilityService(IPersistence persistence)
        {
            _persistence = persistence;
        }

        public async Task CreateAvailability(AvailabilityModel.Request request)
        {
            var doctor = await ValidateDoctor(request.DoctorId);

            var today = DateTime.Today;
            short year = (short)today.Year;
            byte month = (byte)today.Month;

            ValidateDays(request.Days);

            var existingRules = await _persistence.GetFiltered<AvailabilityRule>
                (r => r.DoctorId == request.DoctorId && r.Month == month && r.Year == year && r.Deleted == false);
            if (existingRules != null)
                CheckOverlaps(request.Days, existingRules);
            
            foreach (var dayReq in request.Days)
            {
                var dayOfWeek = ParseDayOfWeek(dayReq.Day);
                var rule = new AvailabilityRule(request.DoctorId, month, year, (byte)dayOfWeek, dayReq.StartTime, dayReq.EndTime);
                GenerateSlotsForRestOfMonth(rule, today);

                if (rule.Slots.Any())
                    await _persistence.Add(rule);     
            }
        }

        public async Task UpdateAvailability(AvailabilityModel.Request request)
        {
            var doctor = await ValidateDoctor(request.DoctorId);

            var today = DateTime.Today;
            short year = (short)today.Year;
            byte month = (byte)today.Month;

            ValidateDays(request.Days);

            var existingRules = await _persistence.GetFiltered<AvailabilityRule>(
                r => r.DoctorId == request.DoctorId && r.Month == month && r.Year == year && r.Deleted == false, "Slots");

            if (existingRules != null)
            {
                foreach (var rule in existingRules)
                {
                    rule.SetDeleted();
                    rule.UpdatedAt = DateTime.UtcNow;
                    await _persistence.Update(rule);

                    if (rule.Slots != null)
                    {
                        foreach (var slot in rule.Slots)
                        {
                            slot.SetDeleted();
                            slot.UpdatedAt = DateTime.UtcNow;
                            await _persistence.Update(slot);
                        }
                    }
                }
            }
            CheckOverlaps(request.Days, new List<AvailabilityRule>());

            foreach (var dayReq in request.Days)
            {
                var dayOfWeek = ParseDayOfWeek(dayReq.Day);
                var rule = new AvailabilityRule(request.DoctorId, month, year, (byte)dayOfWeek, dayReq.StartTime, dayReq.EndTime);

                GenerateSlotsForRestOfMonth(rule, today);

                if (rule.Slots.Any()) 
                    await _persistence.Add(rule);
            }
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
                var parsedDay = (byte)ParseDayOfWeek(req.Day);
                var ruleOverlaps = existingRules.Where(r => r.DayOfWeek == parsedDay);

                foreach (var rule in ruleOverlaps)
                {
                    if (req.StartTime < rule.EndTime && req.EndTime > rule.StartTime)
                        throw new ConflictException(ErrorCodes.AVAILABILITY_CONFLICT, nameof(ErrorCodes.AVAILABILITY_CONFLICT))
                            .WithDetail(nameof(req.Day), $"Ya existe una disponibilidad para el día {req.Day} en el horario solicitado.");
                }
            }
        }

        private void GenerateSlotsForRestOfMonth(AvailabilityRule rule, DateTime today)
        {
            int daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);

            for (int day = today.Day; day <= daysInMonth; day++)
            {
                var date = new DateTime(today.Year, today.Month, day);

                if ((int)date.DayOfWeek == rule.DayOfWeek)
                {
                    var currentTime = rule.StartTime;

                    // Bloques estables de 30 minutos
                    while (currentTime.Add(TimeSpan.FromMinutes(30)) <= rule.EndTime)
                    {
                        var slot = new AvailabilitySlot(rule.Id, date, currentTime, currentTime.Add(TimeSpan.FromMinutes(30)));
                        rule.Slots.Add(slot);
                        currentTime = currentTime.Add(TimeSpan.FromMinutes(30));
                    }
                }
            }
        }

        private DayOfWeek ParseDayOfWeek (string day)
        {
            return day.Trim().ToUpper() switch
            {
                "LUNES" => DayOfWeek.Monday,
                "MARTES" => DayOfWeek.Tuesday,
                "MIERCOLES" => DayOfWeek.Wednesday,
                "JUEVES" => DayOfWeek.Thursday,
                "VIERNES" => DayOfWeek.Friday,
                "SABADO" => DayOfWeek.Saturday,
                "DOMINGO" => DayOfWeek.Sunday,
                _ => throw new ValidationException()
                        .WithDetail(nameof(day), "El día no es válido.")
            };
        }

        private async Task <Doctor> ValidateDoctor (Guid doctorId)
        {
            if (doctorId == Guid.Empty )
                throw new ValidationException() 
                    .WithDetail(nameof(doctorId), "Debe indicar un DoctorId válido.");
            
            return await _persistence.GetById<Doctor>(doctorId)
                ?? throw new EntityNotFoundException(nameof(Doctor));
        }
        #endregion
    }
}
