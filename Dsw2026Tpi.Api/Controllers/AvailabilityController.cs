using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsw2026Tpi.Api.Controllers;

[Route("api/availabilities")]
[Authorize(Policy = Policies.AdminPolicy)]
    public class AvailabilityController : AppController
    {
    private readonly IAvailabilityService _service;

    public AvailabilityController(IAvailabilityService availabilityService)
    {
        _service = availabilityService;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] AvailabilityModel.Request request)
    {
       var response = await _service.CreateAvailability(request);
       return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update([FromBody] AvailabilityModel.Request request)
    {
        var response = await _service.UpdateAvailability(request);
        return Ok(response);
    }
}
