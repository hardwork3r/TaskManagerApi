using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManagerApi.Models;
using TaskManagerApi.Services;

namespace TaskManagerApi.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly UserService _userService;
    private readonly TaskService _taskService;

    public AdminController(UserService userService, TaskService taskService)
    {
        _userService = userService;
        _taskService = taskService;
    }

    private async Task<User?> GetCurrentUser()
    {
        var userId = User.FindFirst("sub")?.Value;
        if (userId == null) return null;
        return await _userService.GetUserByIdAsync(userId);
    }

    private bool IsAdminFromClaims()
    {
        var role = User.FindFirst("role")?.Value
                ?? User.FindFirst(ClaimTypes.Role)?.Value;
        return string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase);
    }

    [HttpGet("users")]
    public async Task<ActionResult<List<User>>> GetAllUsers()
    {
        if (!IsAdminFromClaims())
            return StatusCode(403, new { detail = "Admin access required" });

        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpPut("users/{userId}")]
    public async Task<ActionResult<User>> UpdateUser(
        string userId,
        [FromBody] UserUpdate userUpdate)
    {
        if (!IsAdminFromClaims())
        {
            return StatusCode(403, new { detail = "Admin access required" });
        }

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { detail = "User not found" });
        }

        await _userService.UpdateUserAsync(userId, userUpdate);

        var updatedUser = await _userService.GetUserByIdAsync(userId);
        if (updatedUser != null)
        {
            updatedUser.HashedPassword = null;
        }

        return Ok(updatedUser);
    }

    [HttpDelete("users/{userId}")]
    public async Task<ActionResult> DeleteUser(string userId)
    {
        var currentUser = await GetCurrentUser();
        if (currentUser == null || currentUser.Role != "admin")
        {
            return StatusCode(403, new { detail = "Admin access required" });
        }

        if (userId == currentUser.Id)
        {
            return BadRequest(new { detail = "Cannot delete yourself" });
        }

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { detail = "User not found" });
        }

        await _userService.DeleteUserAsync(userId);
        await _taskService.DeleteTasksByUserIdAsync(userId);

        return Ok(new { message = "User deleted successfully" });
    }
}