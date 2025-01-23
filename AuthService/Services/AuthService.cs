using De.Hsfl.LoomChat.Common.Models;
using De.Hsfl.LoomChat.Auth.Persistence;
using De.Hsfl.LoomChat.Common.Dtos;
using Microsoft.EntityFrameworkCore;

namespace De.Hsfl.LoomChat.Auth.Services
{
    /// <summary>
    /// This service provides business logic for user authentication.
    /// It uses the AuthDbContext to access the user data.
    /// It uses the PasswordHasher to hash and verify passwords.
    /// It uses the JwtUtils to generate JWT tokens.
    /// </summary>

    public class AuthService
    {
        private readonly AuthDbContext _authDbContext;
        private readonly PasswordHasher _passwordHasher;
        private readonly JwtUtils _jwtUtils;

        public AuthService(AuthDbContext authDbContext, PasswordHasher passwordHasher, JwtUtils jwtUtils)
        {
            _authDbContext = authDbContext;
            _passwordHasher = passwordHasher;
            _jwtUtils = jwtUtils;
        }

        // Registers a new user with the given username and password.
        public async Task<RegisterResponse> RegisterAsync(string username, string plainPassword)
        {
            bool exists = await _authDbContext.Users.AnyAsync(u => u.Username == username);
            if (exists) return null;

            var user = new User
            {
                Username = username,
                PasswordHash = _passwordHasher.HashPassword(plainPassword),
                CreatedAt = DateTime.UtcNow
            };

            _authDbContext.Users.Add(user);
            await _authDbContext.SaveChangesAsync();
            var token = _jwtUtils.GenerateToken(user.Id, user.Username);
            return new RegisterResponse(token, user.Id, user.Username);
        }

        // Logs in a user with the given username and password.
        // Returns a LoginResponse with a JWT token if the login was successful.
        public async Task<LoginResponse?> LoginAsync(string username, string plainPassword)
        {
            var user = await _authDbContext.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return null;

            bool valid = _passwordHasher.VerifyPassword(plainPassword, user.PasswordHash);
            if (!valid) return null;

            var token = _jwtUtils.GenerateToken(user.Id, user.Username);
            return new LoginResponse(token, user.Id, user.Username);
        }

        public async Task<GetUsersResponse> GetUsers(GetUsersRequest request)
        {
            var users = await _authDbContext.Users.ToListAsync();
            return new GetUsersResponse(users);
        }
    }
}
