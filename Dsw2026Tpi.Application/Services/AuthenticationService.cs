using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.CrossCutting.Resources;
using Dsw2026Tpi.Data.Identity;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Dsw2026Tpi.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISignInService _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IPersistence _persistence;

    public AuthenticationService(UserManager<ApplicationUser> userManager,
        ISignInService signInManager,
        RoleManager<IdentityRole> roleManager,
        JwtService jwtService,
        ILogger<AuthenticationService> logger,
        IPersistence persistence)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _logger = logger;
        _persistence = persistence;
    }

    public async Task<LoginAdminModel.Response> LoginAdmin(LoginAdminModel.Request request)
    {
        if (!request.Email.IsEmailValid()) throw new ValidationException("Los datos enviados son inválidos.", ErrorCodes.VALIDATION_ERROR)
                .WithDetail(nameof(request.Email), "Debe indicar un email válido");


        if (!request.Password.IsPasswordValid()) throw new ValidationException("Los datos enviados son inválidos.", ErrorCodes.VALIDATION_ERROR)
                .WithDetail(nameof(request.Password), "La contraseña debe tener al menos 8 caracteres.");


        var user = await _userManager.FindByEmailAsync(request.Email) ?? throw new AuthenticationException();
        var result = await _signInManager.CheckPassword(user, request.Password);

        if (!result)
        {
            _logger.LogError("Intento de login fallido para: {Email}", request.Email);
            throw new AuthenticationException();
        }

        var role = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

        if (role != Roles.Administrator)
        {
            throw new AuthenticationException();
        }

        var token = _jwtService.GenerateToken(user.UserName!, role);

        _logger.LogInformation("Inicio de sesion de administrador exitoso: {Email}", request.Email);

        return new LoginAdminModel.Response(
            token,
            role
        );
    }

    public async Task<LoginPatientModel.Response> LoginPatient(LoginPatientModel.Request request)
    {
        if (!request.Email.IsEmailValid())
            throw new ValidationException("Los datos enviados son inválidos.", ErrorCodes.VALIDATION_ERROR)
                .WithDetail(nameof(request.Email), "Debe indicar un email válido.");

        var dniString = request.Dni.ToString();
        if (!request.Dni.IsDniValid())
            throw new ValidationException("Los datos enviados son inválidos.", ErrorCodes.VALIDATION_ERROR)
                .WithDetail(nameof(request.Dni), "El DNI debe tener entre 7 y 8 dígitos.");

        var patient = await _persistence.First<Patient>(p => p.Dni == dniString);

        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null && patient is null)
        {
            user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            var userResult = await _userManager.CreateAsync(user);

            if (!userResult.Succeeded)
                throw new ConflictException("No se pudo registrar el usuario.", ErrorCodes.REGISTER_USER_CONFLICT)
                    .WithDetail(userResult.Errors.Select(e => (e.Code, e.Description)));

            var rol = await _userManager.AddToRoleAsync(user, Roles.Patient);

            if (!rol.Succeeded)
                throw new ConflictException("No se pudo asignar el rol Paciente.", ErrorCodes.REGISTER_USER_CONFLICT)
                    .WithDetail(rol.Errors.Select(error => (error.Code, error.Description)));

            var userId = Guid.Parse(user.Id);

            patient = new Patient(userId, dniString);
            _logger.LogInformation("Paciente registrado automáticamente. DNI: {Dni}", dniString);

            await _persistence.Add(patient);
        }

        if (user is null || patient is null)
        {
            throw new AuthenticationException();
        }
        var authenticatedUserId = Guid.Parse(user.Id);

        if (patient.UserId != authenticatedUserId)
        {
            throw new AuthenticationException();
        }

        var token = _jwtService.GenerateToken(user.UserName!, Roles.Patient);

        _logger.LogInformation("Inicio de sesion de paciente exitoso: {Email}", request.Email);

        return new LoginPatientModel.Response(
            token,
            Roles.Patient
        );
    }

    public async Task<RegisterModel.Response> Register(RegisterModel.Request request)
    {
        if (!request.Email.IsEmailValid()) 
            throw new ValidationException("Los datos enviados son inválidos.", ErrorCodes.REGISTER_USER_INVALID)
                .WithDetail(nameof(request.Email), "Debe indicar un email válido.");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded) 
            throw new ConflictException("No se pudo registrar el usuario.", ErrorCodes.REGISTER_USER_CONFLICT)
                .WithDetail(result.Errors.Select(e => (e.Code, e.Description)));

        _ = await _userManager.AddToRoleAsync(user, Roles.Administrator);

        _logger.LogInformation("Usuario registrado: {Email}", request.Email);

        return new RegisterModel.Response(request.Email);
    }
}
