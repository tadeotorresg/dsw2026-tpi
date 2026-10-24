using Dsw2026Tpi.Application.Dtos;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IPatientService
{
    Task<PatientModel.Response> GetProfile(string dni);

    Task<PatientModel.Response> UpdateProfile(
        string dni,
        PatientModel.Request request);
}
