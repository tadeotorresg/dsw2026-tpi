using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dsw2026Tpi.CrossCutting.Helpers;

namespace Dsw2026Tpi.Api.Controllers;

[Route("api/patients")]
[Authorize(Policy = Policies.PatientPolicy)]
public class PatientController : AppController
{
    private readonly IPatientService _service;

    public PatientController(IPatientService service)
    {
        _service = service;
    }

    [HttpGet("me")]
    [ProducesResponseType(
        typeof(PatientModel.Response),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile()
    {
        var dni = User.GetAuthenticatedDni();

        var response = await _service.GetProfile(dni);

        return Ok(response);
    }

    [HttpPut("me")]
    [ProducesResponseType(
        typeof(PatientModel.Response),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] PatientModel.Request request)
    {
        var dni = User.GetAuthenticatedDni();

        var response = await _service.UpdateProfile(dni, request);

        return Ok(response);
    }
}
