using Microsoft.AspNetCore.Mvc;
using De.Hsfl.LoomChat.Auth.Services;
using De.Hsfl.LoomChat.Auth.Dtos;

namespace De.Hsfl.LoomChat.Auth.Controllers
{
    /// <summary>
    /// Provides endpoints for user registration and login.
    /// Uses AuthService to handle credential checks, hashing, and JWT creation.
    /// </summary>
    
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            bool success = await _authService.RegisterAsync(request.Username, request.Password);
            return success ? Ok("Registered.") : BadRequest("Username already taken.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var response = await _authService.LoginAsync(request.Username, request.Password);
            return response == null ? Unauthorized("Invalid credentials.") : Ok(response);
        }
    }
}
