using Dsw2026Tpi.Data.Identity;

namespace Dsw2026Tpi.Application.Interfaces;

public interface ISignInService
{
    Task<bool> CheckPassword(ApplicationUser user, string password);
}
