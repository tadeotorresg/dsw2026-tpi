using Dsw2026Tpi.Application.Dtos;
namespace Dsw2026Tpi.Application.Interfaces
{
    public interface IAvailabilityService
    {
        Task <IEnumerable<AvailabilityModel.Response>> CreateAvailability(AvailabilityModel.Request request);
        Task<IEnumerable<AvailabilityModel.Response>> UpdateAvailability(AvailabilityModel.Request request);
    }
}
