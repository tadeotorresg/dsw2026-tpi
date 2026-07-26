using Dsw2026Tpi.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Application.Interfaces
{
    public interface IAvailabilityService
    {
        Task CreateAvailability(AvailabilityModel.Request request);
        Task UpdateAvailability(AvailabilityModel.Request request);

    }
}
