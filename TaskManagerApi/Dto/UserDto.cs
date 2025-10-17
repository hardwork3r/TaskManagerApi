namespace TaskManagerApi.Dto
{
    public class TaskWithUsersDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "todo";
        public string Priority { get; set; } = "medium";
        public string? DueDate { get; set; }
        public List<string> Tags { get; set; } = new();
        public string UserId { get; set; } = string.Empty;
        public List<UserDto> AssignedUsers { get; set; } = new();
        public string CreatedAt { get; set; } = DateTime.UtcNow.ToString("o");
        public List<AttachmentDto> Attachments { get; set; } = new();
    }

    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class AttachmentDto
    {
        public string Id { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string GridFsId { get; set; } = string.Empty;
        public string UploadedAt { get; set; } = DateTime.UtcNow.ToString("o");
    }
}
