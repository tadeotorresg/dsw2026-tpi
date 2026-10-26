using Dsw2026Tpi.Api.Configurations;
using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Dsw2026Tpi.CrossCutting.Identity;
using Microsoft.AspNetCore.Authorization;
using Dsw2026Tpi.CrossCutting.Exceptions;

namespace Dsw2026Tpi.Api.Controllers;

[Route("api/auth")]
public class AuthenticationController : AppController
{
    private readonly IAuthenticationService _authenticationService;

    public AuthenticationController(IAuthenticationService authenticationService) 
    {
        _authenticationService = authenticationService;
    }

    [HttpPost("admin/register")]
    [Authorize(Policy = Policies.AdminPolicy)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterModel.Request request)
    {
        var result = await _authenticationService.Register(request);
        return Ok(result.Email); 
    }

    [HttpPost("admin/login")]
    [EnableRateLimiting(RateLimitingConfigurationExtensions.AdminLogin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginAdminModel.Request request)
    {
        var result = await _authenticationService.LoginAdmin(request);
        return Ok(result);
    }

    [HttpPost("patient/login")]
    [EnableRateLimiting(RateLimitingConfigurationExtensions.PatientLogin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LoginPatient([FromBody] LoginPatientModel.Request request)
    {
        var result = await _authenticationService.LoginPatient(request);
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        var username = User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(username))
            throw new AuthenticationException();

        await _authenticationService.Logout(username);

        return NoContent();
    }
}
