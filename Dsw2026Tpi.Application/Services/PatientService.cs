using Dsw2026Tpi.Application.Dtos;
using Dsw2026Tpi.Application.Interfaces;
using Dsw2026Tpi.CrossCutting.Exceptions;
using Dsw2026Tpi.Domain.Entities;
using Dsw2026Tpi.Domain.Interfaces;

namespace Dsw2026Tpi.Application.Services;

public class PatientService : IPatientService
{
    private readonly IPersistence _persistence;

    public PatientService(IPersistence persistence)
    {
        _persistence = persistence;
    }

    public async Task<PatientModel.Response> GetProfile(string dni)
    {
        var patient = await GetPatient(dni);

        return MapResponse(patient);
    }

    public async Task<PatientModel.Response> UpdateProfile(
        string dni,
        PatientModel.Request request)
    {
        var fullName = request.FullName?.Trim();

        if (string.IsNullOrWhiteSpace(fullName)
            || fullName.Length < 3
            || fullName.Length > 150)
        {
            throw new ValidationException()
                .WithDetail("fullName", "El nombre completo debe tener entre 3 y 150 caracteres.");
        }

        var patient = await GetPatient(dni);

        patient.UpdateProfile(fullName);

        await _persistence.Update(patient);

        return MapResponse(patient);
    }

    private async Task<Patient> GetPatient(string dni)
    {
        return await _persistence.First<Patient>( patient => patient.Dni == dni)
            ?? throw new EntityNotFoundException(nameof(Patient));
    }

    private static PatientModel.Response MapResponse(Patient patient)
    {
        return new PatientModel.Response(
            patient.Id,
            long.Parse(patient.Dni),
            patient.FullName);
    }
}
