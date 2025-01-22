using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace De.Hsfl.LoomChat.File.Helpers
{
    /// <summary>
    /// Helpers to extract user info from JWT claims
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst(JwtRegisteredClaimNames.Sub);
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        public static string GetUsername(this ClaimsPrincipal user)
        {
            var claim = user.FindFirst(JwtRegisteredClaimNames.UniqueName);
            return claim?.Value ?? string.Empty;
        }
    }
}
