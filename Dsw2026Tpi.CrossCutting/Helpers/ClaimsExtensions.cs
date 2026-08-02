using Dsw2026Tpi.CrossCutting.Exceptions;
using System.Security.Claims;

namespace Dsw2026Tpi.CrossCutting.Helpers
{
    public static class ClaimsExtensions
    {
        public const string DniClaim = "dni";
        public static string GetAuthenticatedDni(this ClaimsPrincipal user, long? requestedDni = null)
        {
            var tokenDni = user.FindFirst(DniClaim)?.Value;

            if (string.IsNullOrWhiteSpace(tokenDni))
                throw new AuthenticationException();

            if (requestedDni.HasValue && requestedDni.Value.ToString() != tokenDni)
                throw new AuthorizationException();

            return tokenDni;
        }
    }
}
