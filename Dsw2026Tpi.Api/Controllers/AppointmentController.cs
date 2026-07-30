using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers
{

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
        [ProducesResponseType(StatusCodes.Status201Created)]
        
       
        public async Task<IActionResult> Create([FromBody] AppointmentModel.Request request)
        {
            var response = await _appointmentService.CreateAppointment(request);
            return StatusCode(StatusCodes.Status201Created, response);
        }

        [HttpGet("patient")]
        [Authorize(Policy = Policies.PatientPolicy)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        
        public async Task<IActionResult> GetPatientAppointments([FromQuery] long dni)
        {
            var response = await _appointmentService.GetPatientAppointments(dni);
            return Ok(response);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = Policies.PatientPolicy)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
       
        public async Task<IActionResult> Cancel(Guid id)
        {
            await _appointmentService.CancelAppointment(id);
            return NoContent();
        }

        [HttpGet]
        [Authorize(Policy = Policies.AdminPolicy)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDailyAppointments([FromQuery] DateOnly date)
        {
            var response = await _appointmentService.GetDailyAppointments(date);
            return Ok(response);
        }

        [HttpGet("search")]
        [Authorize(Policy = Policies.AdminPolicy)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchAppointments(
            [FromQuery] Guid? specialityId,
            [FromQuery] Guid? doctorId,
            [FromQuery] string? dni,
            [FromQuery] DateOnly? date,
            [FromQuery] int pageSize = 10,
            [FromQuery] int pageIndex = 1)
        {
            var request =new SearchModel.Request(specialityId,doctorId, dni, date, pageSize, pageIndex);
            var response = await _appointmentService.SearchAppointments(request);

            return Ok(response);
        }
    }
}

