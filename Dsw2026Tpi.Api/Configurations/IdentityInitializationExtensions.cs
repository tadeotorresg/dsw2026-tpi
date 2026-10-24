using Dsw2026Tpi.CrossCutting.Helpers;
using Dsw2026Tpi.CrossCutting.Identity;
using Dsw2026Tpi.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace Dsw2026Tpi.Api.Configurations;

public static class IdentityInitializationExtensions
{
    public static async Task InitializeIdentityAsync(
        this WebApplication app)
    {
        var email = app.Configuration["InitialAdmin:Email"]?.Trim();

        if (!email.IsEmailValid())
        {
            throw new InvalidOperationException(
                "Debe configurar un email válido en InitialAdmin:Email.");
        }

        await using var scope = app.Services.CreateAsyncScope();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        var context = scope.ServiceProvider
            .GetRequiredService<AuthenticationDbContext>();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        foreach (var roleName in new[]
        {
            Roles.Administrator,
            Roles.Patient
        })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await roleManager.CreateAsync(
                    new IdentityRole(roleName));

                EnsureSuccess(
                    roleResult,
                    $"No se pudo crear el rol {roleName}");
            }
        }

        var existingUser = await userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            var isAdministrator = await userManager.IsInRoleAsync(
                existingUser,
                Roles.Administrator);

            var hasPassword = await userManager.HasPasswordAsync(
                existingUser);

            if (existingUser.Deleted
                || !isAdministrator
                || !hasPassword)
            {
                throw new InvalidOperationException(
                    "El email inicial pertenece a una cuenta "
                    + "que no es un administrador habilitado "
                    + "con contraseña. Configure otro email.");
            }

            await transaction.CommitAsync();

            return;
        }

        var password = app.Configuration["InitialAdmin:Password"];

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Debe configurar InitialAdmin:Password "
                + "para crear el administrador inicial.");
        }

        var now = DateTime.Now;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            CreatedAt = now,
            UpdatedAt = now
        };

        var createResult = await userManager.CreateAsync(
            user,
            password);

        EnsureSuccess(
            createResult,
            "No se pudo crear el administrador inicial");

        var assignRoleResult = await userManager.AddToRoleAsync(
            user,
            Roles.Administrator);

        EnsureSuccess(
            assignRoleResult,
            "No se pudo asignar el rol de administrador");

        await transaction.CommitAsync();

        app.Logger.LogInformation(
            "Administrador inicial creado correctamente.");
    }

    private static void EnsureSuccess(
        IdentityResult result,
        string operation)
    {
        if (result.Succeeded)
            return;

        var errors = string.Join(
            "; ",
            result.Errors.Select(error => error.Description));

        throw new InvalidOperationException(
            $"{operation}: {errors}");
    }
}
