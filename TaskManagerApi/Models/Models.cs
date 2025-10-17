using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace TaskManagerApi.Models;

// ==================== USER MODELS ====================

public class User
{
    [BsonId]
    [BsonElement("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("email")]
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [BsonElement("name")]
    [Required]
    public string Name { get; set; } = string.Empty;

    [BsonElement("role")]
    public string Role { get; set; } = "user";

    [BsonElement("created_at")]
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");

    [BsonElement("hashed_password")]
    [BsonIgnoreIfNull]
    public string? HashedPassword { get; set; }
}

public class UserCreate
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Role { get; set; } = "user";
}

public class UserLogin
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class UserUpdate
{
    public string? Name { get; set; }
    [EmailAddress]
    public string? Email { get; set; }
    public string? Role { get; set; }
}

public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "bearer";
    public User User { get; set; } = new();
}

// ==================== TASK MODELS ====================

public class TaskItem
{
    [BsonId]
    [BsonElement("id")]
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("title")]
    [Required]
    [System.Text.Json.Serialization.JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("description")]
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("status")]
    [System.Text.Json.Serialization.JsonPropertyName("status")]
    public string Status { get; set; } = "todo";

    [BsonElement("priority")]
    [System.Text.Json.Serialization.JsonPropertyName("priority")]
    public string Priority { get; set; } = "medium";

    [BsonElement("due_date")]
    [System.Text.Json.Serialization.JsonPropertyName("dueDate")]
    public string? DueDate { get; set; }

    [BsonElement("tags")]
    [System.Text.Json.Serialization.JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();

    [BsonElement("user_id")]
    [Required]
    [System.Text.Json.Serialization.JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("assigned_users")]
    [System.Text.Json.Serialization.JsonPropertyName("assignedUsers")]
    public List<string> AssignedUsers { get; set; } = new();

    [BsonElement("attachments")]
    [System.Text.Json.Serialization.JsonPropertyName("attachments")]
    public List<TaskAttachment> Attachments { get; set; } = new();

    [BsonElement("created_at")]
    [System.Text.Json.Serialization.JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");
}

public class TaskAttachment
{
    [BsonElement("id")]
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [BsonElement("file_name")]
    [System.Text.Json.Serialization.JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [BsonElement("file_size")]
    [System.Text.Json.Serialization.JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    [BsonElement("content_type")]
    [System.Text.Json.Serialization.JsonPropertyName("contentType")]
    public string ContentType { get; set; } = string.Empty;

    [BsonElement("gridfs_id")]
    [System.Text.Json.Serialization.JsonPropertyName("gridFsId")]
    public string GridFsId { get; set; } = string.Empty;

    [BsonElement("uploaded_at")]
    [System.Text.Json.Serialization.JsonPropertyName("uploadedAt")]
    public string UploadedAt { get; set; } = DateTime.UtcNow.ToString("o");
}

public class TaskCreate
{
    [Required]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "todo";
    public string Priority { get; set; } = "medium";
    public string? DueDate { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<string> AssignedUsers { get; set; } = new();
}

public class TaskUpdate
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public string? DueDate { get; set; }
    public List<string>? Tags { get; set; }
    public List<string>? AssignedUsers { get; set; }
}