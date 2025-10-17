using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManagerApi.Models;
using TaskManagerApi.Services;

namespace TaskManagerApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;
    private readonly AuthService _authService;

    public AuthController(UserService userService, AuthService authService)
    {
        _userService = userService;
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<TokenResponse>> Register([FromBody] UserCreate userCreate)
    {
        var existingUser = await _userService.GetUserByEmailAsync(userCreate.Email);
        if (existingUser != null)
        {
            return BadRequest(new { detail = "Email already registered" });
        }

        var user = new User
        {
            Email = userCreate.Email,
            Name = userCreate.Name,
            Role = userCreate.Role,
            HashedPassword = _authService.HashPassword(userCreate.Password)
        };

        await _userService.CreateUserAsync(user);

        var accessToken = _authService.CreateAccessToken(user.Id, user.Role);

        user.HashedPassword = null;

        return Ok(new TokenResponse
        {
            AccessToken = accessToken,
            TokenType = "bearer",
            User = user
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] UserLogin userLogin)
    {
        var user = await _userService.GetUserByEmailAsync(userLogin.Email);
        if (user == null)
        {
            return Unauthorized(new { detail = "Invalid email or password" });
        }

        if (!_authService.VerifyPassword(userLogin.Password, user.HashedPassword!))
        {
            return Unauthorized(new { detail = "Invalid email or password" });
        }

        var accessToken = _authService.CreateAccessToken(user.Id, user.Role);

        user.HashedPassword = null;

        return Ok(new TokenResponse
        {
            AccessToken = accessToken,
            TokenType = "bearer",
            User = user
        });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<User>> GetMe()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        if (userId == null)
        {
            return Unauthorized(new { detail = "Invalid authentication credentials" });
        }

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return Unauthorized(new { detail = "User not found" });
        }

        user.HashedPassword = null;
        return Ok(user);
    }
}