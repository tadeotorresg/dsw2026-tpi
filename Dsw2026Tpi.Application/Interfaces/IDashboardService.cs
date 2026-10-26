using Dsw2026Tpi.Application.Dtos;

namespace Dsw2026Tpi.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardModel.Response> GetSummary();
}
