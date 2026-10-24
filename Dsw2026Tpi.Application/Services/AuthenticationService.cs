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
using Microsoft.EntityFrameworkCore;

namespace Dsw2026Tpi.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISignInService _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IPersistence _persistence;
    private readonly AuthenticationDbContext _authenticationDbContext;
    public AuthenticationService(UserManager<ApplicationUser> userManager,
        ISignInService signInManager,
        RoleManager<IdentityRole> roleManager,
        JwtService jwtService,
        ILogger<AuthenticationService> logger,
        IPersistence persistence,
        AuthenticationDbContext authenticationDbContext)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _logger = logger;
        _persistence = persistence;
        _authenticationDbContext = authenticationDbContext;
    }

    public async Task<LoginAdminModel.Response> LoginAdmin(
    LoginAdminModel.Request request)
    {
        if (!request.Email.IsEmailValid())
            throw new ValidationException()
                .WithDetail(nameof(request.Email), "Debe indicar un email válido.");

        if (!request.Password.IsPasswordValid())
            throw new ValidationException()
                .WithDetail(nameof(request.Password), "La contraseña debe tener al menos 8 caracteres.");

        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null || user.Deleted)
            throw new AuthenticationException();

        var validPassword = await _signInManager.CheckPassword(
            user,
            request.Password);

        if (!validPassword)
        {
            _logger.LogWarning( "Intento de login administrativo fallido para: {Email}", request.Email);

            throw new AuthenticationException();
        }

        var isAdministrator = await _userManager.IsInRoleAsync(
            user,
            Roles.Administrator);

        if (!isAdministrator)
            throw new AuthenticationException();

        var token = _jwtService.GenerateToken(
            user.UserName!,
            Roles.Administrator);

        _logger.LogInformation(
            "Inicio de sesión de administrador exitoso: {Email}",
            request.Email);

        return new LoginAdminModel.Response(
            token,
            Roles.Administrator.ToUpperInvariant());
    }

    public async Task<LoginPatientModel.Response> LoginPatient(LoginPatientModel.Request request)
    {
        if (!request.Email.IsEmailValid())
            throw new ValidationException()
                .WithDetail(nameof(request.Email), "Debe indicar un email válido.");

        var dniString = request.Dni.ToString();
        if (!request.Dni.IsDniValid())
            throw new ValidationException()
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
                throw new ConflictException(ErrorCodes.REGISTER_USER_CONFLICT, nameof(ErrorCodes.REGISTER_USER_CONFLICT))
                    .WithDetail(userResult.Errors.Select(e => (e.Code, e.Description)));

            var rol = await _userManager.AddToRoleAsync(user, Roles.Patient);

            if (!rol.Succeeded)
                throw new ConflictException(ErrorCodes.REGISTER_USER_CONFLICT, nameof(ErrorCodes.REGISTER_USER_CONFLICT))
                    .WithDetail(rol.Errors.Select(error => (error.Code, error.Description)));

            var userId = Guid.Parse(user.Id);

            patient = new Patient(userId, dniString);

            await _persistence.Add(patient);

            _logger.LogInformation("Paciente registrado automáticamente. DNI: {Dni}", dniString);
        }

        if (user is null || user.Deleted || patient is null)
            throw new AuthenticationException();

        if (!Guid.TryParse(user.Id, out var authenticatedUserId)
            || patient.UserId != authenticatedUserId)
            throw new AuthenticationException();

        var isPatient = await _userManager.IsInRoleAsync(user, Roles.Patient);

        if (!isPatient)
            throw new AuthenticationException();

        var token = _jwtService.GenerateToken(
            user.UserName!,
            Roles.Patient,
            dniString);

        _logger.LogInformation("Inicio de sesion de paciente exitoso: {Email}", request.Email);

        return new LoginPatientModel.Response(
            token,
            Roles.Patient.ToUpperInvariant());
    }

    public async Task<RegisterModel.Response> Register(
    RegisterModel.Request request)
    {
        if (!request.Email.IsEmailValid())
            throw new ValidationException()
                .WithDetail(nameof(request.Email), "Debe indicar un email válido.");

        if (!request.Password.IsPasswordValid())
            throw new ValidationException()
                .WithDetail(nameof(request.Password), "La contraseña debe tener al menos 8 caracteres.");

        await using var transaction =
            await _authenticationDbContext.Database.BeginTransactionAsync();

        var now = DateTime.Now;

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            CreatedAt = now,
            UpdatedAt = now
        };

        var createResult = await _userManager.CreateAsync(
            user,
            request.Password);

        if (!createResult.Succeeded)
        {
            throw new ConflictException(
                ErrorCodes.REGISTER_USER_CONFLICT,
                nameof(ErrorCodes.REGISTER_USER_CONFLICT))
                .WithDetail(
                    createResult.Errors.Select(
                        error => (error.Code, error.Description)));
        }

        var roleResult = await _userManager.AddToRoleAsync(
            user,
            Roles.Administrator);

        if (!roleResult.Succeeded)
        {
            throw new ConflictException(
                ErrorCodes.REGISTER_USER_CONFLICT,
                nameof(ErrorCodes.REGISTER_USER_CONFLICT))
                .WithDetail(
                    roleResult.Errors.Select(
                        error => (error.Code, error.Description)));
        }

        await transaction.CommitAsync();

        _logger.LogInformation(
            "Administrador registrado: {Email}",
            user.Email);

        return new RegisterModel.Response(user.Email!);
    }
}
