using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace De.Hsfl.LoomChat.Auth.Services
{
    /// <summary>
    /// Utility class for generating JWT tokens
    /// </summary>
    public class JwtUtils
    {
        private readonly IConfiguration _config;

        public JwtUtils(IConfiguration config)
        {
            _config = config;
        }

        // Generates a signed JWT token containing user information (ID, username)
        // The token is signed using a secret key from configuration
        // The token expires after 7 days
        public string GenerateToken(int userId, string username)
        {
            string secret = _config["Jwt:Secret"]
                ?? throw new InvalidOperationException("JWT Secret not found in config");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, username),
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
