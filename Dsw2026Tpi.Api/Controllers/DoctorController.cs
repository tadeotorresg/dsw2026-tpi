using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

[Route("api/doctors")]
public class DoctorController : AppController
{
    private readonly IDoctorService _service;
    public DoctorController(IDoctorService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Roles = Roles.Administrator + "," + Roles.Patient)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery]int pageSize = 10, [FromQuery]int pageIndex = 1, [FromQuery]string? name = null, [FromQuery] Guid? specialtyId = null)
    {
        var doctors = await _service.GetAll(pageSize, pageIndex, name, specialtyId, onlyActive: !User.IsInRole(Roles.Administrator));
        return Ok(doctors);
    }

    [HttpGet("{id:guid}/availabilities")]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailabilities(Guid id)
    {
        var availabilities =await _service.GetAvailabilities(id);

        return Ok(availabilities);
    }
    [HttpGet("{id:guid}/slots")]
    [Authorize(Roles = Roles.Administrator + "," + Roles.Patient)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailableSlots(
    Guid id,
    [FromQuery] DateOnly? fromDate = null,
    [FromQuery] DateOnly? toDate = null)
    {
        var slots = await _service.GetAvailableSlots(
            id,
            fromDate,
            toDate);

        return Ok(slots);
    }

    [HttpPost]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] DoctorModel.Request request)
    {
        var doctor =await _service.CreateDoctor(request);

        return StatusCode(
            StatusCodes.Status201Created,
            doctor);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] DoctorModel.Request request)
    {
        var doctor =await _service.UpdateDoctor(id, request);

        return Ok(doctor);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteDoctor(id);

        return Ok("ok");
    }
}
