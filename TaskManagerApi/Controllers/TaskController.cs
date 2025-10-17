using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManagerApi.Models;
using TaskManagerApi.Dto;
using TaskManagerApi.Services;
using System.Net.Mail;

namespace TaskManagerApi.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
public class TaskController : ControllerBase
{
    private readonly TaskService _taskService;
    private readonly UserService _userService;
    private readonly FileService _fileService;
    private const long MaxFileSize = 100 * 1024 * 1024;
    public TaskController(TaskService taskService, UserService userService, FileService fileService)
    {
        _taskService = taskService;
        _userService = userService;
        _fileService = fileService;
    }

    private async Task<User?> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        if (userId == null)
        {
            Console.WriteLine("No user ID found in claims");
            return null;
        }

        Console.WriteLine($"Looking for user with ID: {userId}");
        var user = await _userService.GetUserByIdAsync(userId);

        if (user == null)
        {
            Console.WriteLine($"User not found in database: {userId}");
        }
        else
        {
            Console.WriteLine($"User found: {user.Email}");
        }

        return user;
    }

    [HttpGet]
    public async Task<ActionResult<List<TaskWithUsersDto>>> GetTasks(
       [FromQuery] string? status = null,
       [FromQuery] string? priority = null,
       [FromQuery] string? tag = null,
       [FromQuery] string? search = null)
    {
        var currentUser = await GetCurrentUser();
        if (currentUser == null)
            return Unauthorized(new { detail = "User not found" });

        var userId = currentUser.Role == "admin" ? null : currentUser.Id;

        var tasks = await _taskService.GetTasksAsync(userId, status, priority, tag, search);

        var tasksWithUsers = new List<TaskWithUsersDto>();
        foreach (var task in tasks)
        {
            var assignedUsers = new List<UserDto>();
            foreach (var userIdAssigned in task.AssignedUsers)
            {
                var user = await _userService.GetUserByIdAsync(userIdAssigned);
                if (user != null)
                {
                    assignedUsers.Add(new UserDto { Id = user.Id, Name = user.Name });
                }
            }

            var attachments = task.Attachments?.Select(a => new AttachmentDto
            {
                Id = a.Id,
                FileName = a.FileName,
                FileSize = a.FileSize,
                ContentType = a.ContentType,
                GridFsId = a.GridFsId,
                UploadedAt = a.UploadedAt
            }).ToList() ?? new List<AttachmentDto>();

            tasksWithUsers.Add(new TaskWithUsersDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Priority = task.Priority,
                DueDate = task.DueDate,
                Tags = task.Tags,
                UserId = task.UserId,
                AssignedUsers = assignedUsers,
                CreatedAt = task.CreatedAt,
                Attachments = attachments
            });
        }

        return Ok(tasksWithUsers);
    }


    [HttpPost]
    public async Task<ActionResult<TaskWithUsersDto>> CreateTask([FromBody] TaskCreate taskCreate)
    {
        var currentUser = await GetCurrentUser();
        if (currentUser == null)
        {
            return Unauthorized(new { detail = "User not found" });
        }

        var assignedUsers = taskCreate.AssignedUsers?.Count > 0
            ? taskCreate.AssignedUsers
            : new List<string>();

        if (!assignedUsers.Contains(currentUser.Id))
        {
            assignedUsers.Add(currentUser.Id);
        }

        var task = new TaskItem
        {
            Title = taskCreate.Title,
            Description = taskCreate.Description,
            Status = taskCreate.Status,
            Priority = taskCreate.Priority,
            DueDate = taskCreate.DueDate,
            Tags = taskCreate.Tags,
            UserId = currentUser.Id,
            AssignedUsers = assignedUsers
        };

        await _taskService.CreateTaskAsync(task);

        var assignedUsersDto = new List<UserDto>();
        foreach (var userIdAssigned in assignedUsers)
        {
            var user = await _userService.GetUserByIdAsync(userIdAssigned);
            if (user != null)
            {
                assignedUsersDto.Add(new UserDto { Id = user.Id, Name = user.Name });
            }
        }

        var dto = new TaskWithUsersDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            DueDate = task.DueDate,
            Tags = task.Tags,
            UserId = task.UserId,
            AssignedUsers = assignedUsersDto,
            CreatedAt = task.CreatedAt
        };

        return Ok(dto);
    }


    [HttpGet("{taskId}")]
    public async Task<ActionResult<TaskItem>> GetTask(string taskId)
    {
        var currentUser = await GetCurrentUser();
        if (currentUser == null)
        {
            return Unauthorized(new { detail = "User not found" });
        }

        var task = await _taskService.GetTaskByIdAsync(taskId);
        if (task == null)
        {
            return NotFound(new { detail = "Task not found" });
        }

        if (currentUser.Role != "admin" && task.UserId != currentUser.Id)
        {
            return Forbid();
        }

        return Ok(task);
    }

    [HttpPut("{taskId}")]
    public async Task<ActionResult<TaskWithUsersDto>> UpdateTask(string taskId, [FromBody] TaskUpdate taskUpdate)
    {
        var currentUser = await GetCurrentUser();
        if (currentUser == null)
            return Unauthorized(new { detail = "User not found" });

        var task = await _taskService.GetTaskByIdAsync(taskId);
        if (task == null)
            return NotFound(new { detail = "Task not found" });

        var isOwner = task.UserId == currentUser.Id;
        var isAssigned = task.AssignedUsers?.Contains(currentUser.Id) == true;
        var isAdmin = currentUser.Role == "admin";

        if (!isAdmin && !isOwner && !isAssigned)
            return Forbid();

        if (!isAdmin)
        {
            if (taskUpdate.Status == null)
                return BadRequest(new { detail = "Status field is required for update" });

            await _taskService.UpdateTaskStatusAsync(taskId, taskUpdate.Status);
        }
        else
        {
            await _taskService.UpdateTaskAsync(taskId, taskUpdate);
        }

        var updatedTask = await _taskService.GetTaskByIdAsync(taskId);

        var assignedUsers = new List<UserDto>();
        foreach (var userId in updatedTask.AssignedUsers)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user != null)
                assignedUsers.Add(new UserDto { Id = user.Id, Name = user.Name });
        }

        var attachments = updatedTask.Attachments?.Select(a => new AttachmentDto
        {
            Id = a.Id,
            FileName = a.FileName,
            FileSize = a.FileSize,
            ContentType = a.ContentType,
            UploadedAt = a.UploadedAt
        }).ToList() ?? new List<AttachmentDto>();

        return Ok(new TaskWithUsersDto
        {
            Id = updatedTask.Id,
            Title = updatedTask.Title,
            Description = updatedTask.Description,
            Status = updatedTask.Status,
            Priority = updatedTask.Priority,
            DueDate = updatedTask.DueDate,
            Tags = updatedTask.Tags,
            UserId = updatedTask.UserId,
            AssignedUsers = assignedUsers,
            CreatedAt = updatedTask.CreatedAt,
            Attachments = attachments
        });
    }

    [HttpDelete("{taskId}")]
    public async Task<ActionResult> DeleteTask(string taskId)
    {
        var currentUser = await GetCurrentUser();
        if (currentUser == null)
        {
            return Unauthorized(new { detail = "User not found" });
        }

        var task = await _taskService.GetTaskByIdAsync(taskId);
        if (task == null)
        {
            return NotFound(new { detail = "Task not found" });
        }

        if (currentUser.Role != "admin" && task.UserId != currentUser.Id)
        {
            return Forbid();
        }

        foreach (var attachment in task.Attachments)
        {
            try
            {
                await _fileService.DeleteFileAsync(attachment.GridFsId);
            }
            catch { }
        }

        await _taskService.DeleteTaskAsync(taskId);
        return Ok(new { message = "Task deleted successfully" });
    }

    [HttpPost("{taskId}/attachments")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<ActionResult<TaskAttachment>> UploadAttachment(string taskId, IFormFile file)
    {
        var currentUser = await GetCurrentUser();
        if (currentUser == null)
        {
            return Unauthorized(new { detail = "User not found" });
        }

        var task = await _taskService.GetTaskByIdAsync(taskId);
        if (task == null)
        {
            return NotFound(new { detail = "Task not found" });
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { detail = "No file uploaded" });
        }

        if (file.Length > MaxFileSize)
        {
            return BadRequest(new { detail = "File size exceeds 100MB limit" });
        }

        using var stream = file.OpenReadStream();
        var gridFsId = await _fileService.UploadFileAsync(stream, file.FileName, file.ContentType);

        var attachment = new TaskAttachment
        {
            FileName = file.FileName,
            FileSize = file.Length,
            ContentType = file.ContentType,
            GridFsId = gridFsId
        };

        task.Attachments.Add(attachment);
        await _taskService.UpdateTaskAttachmentsAsync(taskId, task.Attachments);

        return Ok(attachment);
    }

    [HttpGet("{taskId}/attachments/{attachmentId}")]
    public async Task<IActionResult> DownloadAttachment(string taskId, string attachmentId)
    {
        var currentUser = await GetCurrentUser();
        if (currentUser == null)
        {
            return Unauthorized(new { detail = "User not found" });
        }

        var task = await _taskService.GetTaskByIdAsync(taskId);
        if (task == null)
        {
            return NotFound(new { detail = "Task not found" });
        }

        if (currentUser.Role != "admin" && task.UserId != currentUser.Id && !task.AssignedUsers.Contains(currentUser.Id))
        {
            return Forbid();
        }

        var attachment = task.Attachments.FirstOrDefault(a => a.Id == attachmentId);
        if (attachment == null)
        {
            return NotFound(new { detail = "Attachment not found" });
        }

        try
        {
            var (stream, fileName, contentType) = await _fileService.DownloadFileAsync(attachment.GridFsId);
            return File(stream, contentType, fileName);
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { detail = "File not found in storage" });
        }
    }

    [HttpDelete("{taskId}/attachments/{attachmentId}")]
    public async Task<ActionResult> DeleteAttachment(string taskId, string attachmentId)
    {
        var currentUser = await GetCurrentUser();
        if (currentUser == null)
        {
            return Unauthorized(new { detail = "User not found" });
        }

        var task = await _taskService.GetTaskByIdAsync(taskId);
        if (task == null)
        {
            return NotFound(new { detail = "Task not found" });
        }

        if (currentUser.Role != "admin" && task.UserId != currentUser.Id)
        {
            return Forbid();
        }

        var attachment = task.Attachments.FirstOrDefault(a => a.Id == attachmentId);
        if (attachment == null)
        {
            return NotFound(new { detail = "Attachment not found" });
        }

        try
        {
            await _fileService.DeleteFileAsync(attachment.GridFsId);
        }
        catch { }

        task.Attachments.Remove(attachment);
        await _taskService.UpdateTaskAttachmentsAsync(taskId, task.Attachments);

        return Ok(new { message = "Attachment deleted successfully" });
    }
}