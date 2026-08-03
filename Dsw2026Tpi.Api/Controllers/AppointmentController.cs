using Dsw2026Tpi.Api.Configurations;
using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Dsw2026Tpi.CrossCutting.Helpers;

namespace Dsw2026Tpi.Api.Controllers;


[Route("api/appointments")]
public class AppointmentController : AppController 
{
    private readonly IAppointmentService _appointmentService;
    public AppointmentController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    [HttpPost]
    [Authorize(Policy = Policies.PatientPolicy)]
    [EnableRateLimiting(RateLimitingConfigurationExtensions.AppointmentBooking)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] AppointmentModel.Request request)
    {
        User.GetAuthenticatedDni(request.Patient?.Dni);
        var response = await _appointmentService.CreateAppointment(request);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("patient")]
    [Authorize(Policy = Policies.PatientPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPatientAppointments([FromQuery] long? dni)
    {
        var dniDelToken = User.GetAuthenticatedDni(dni);
        var response = await _appointmentService.GetPatientAppointments(long.Parse(dniDelToken));
        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.PatientPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var dniDelToken = User.GetAuthenticatedDni();
        await _appointmentService.CancelAppointment(id, dniDelToken);
        return Ok("ok");
    }

    [HttpGet]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDailyAppointments([FromQuery] DateOnly? date)
    {
        var response = await _appointmentService.GetDailyAppointments(date);
        return Ok(response);
    }

    [HttpGet("search")]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchAppointments(
        [FromQuery] Guid? specialtyId,
        [FromQuery] Guid? doctorId,
        [FromQuery] long? dni,
        [FromQuery] DateOnly? date,
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageIndex = 1)
    {
        var request =new SearchModel.Request(specialtyId,doctorId, dni, date, pageSize, pageIndex);
        var response = await _appointmentService.SearchAppointments(request);
        return Ok(response);
    }
}
