using MongoDB.Driver;
using TaskManagerApi.Models;

namespace TaskManagerApi.Services;

public class TaskService
{
    private readonly IMongoCollection<TaskItem> _tasks;

    public TaskService(IMongoDatabase database)
    {
        _tasks = database.GetCollection<TaskItem>("tasks");
    }

    public async Task<List<TaskItem>> GetTasksAsync(
        string? userId = null,
        string? status = null,
        string? priority = null,
        string? tag = null,
        string? search = null)
    {
        var filterBuilder = Builders<TaskItem>.Filter;
        var filters = new List<FilterDefinition<TaskItem>>();

        if (!string.IsNullOrEmpty(userId))
        {
            var userFilter = filterBuilder.Or(
                filterBuilder.Eq(t => t.UserId, userId),
                filterBuilder.AnyEq(t => t.AssignedUsers, userId)
            );
            filters.Add(userFilter);
        }

        if (!string.IsNullOrEmpty(status))
            filters.Add(filterBuilder.Eq(t => t.Status, status));

        if (!string.IsNullOrEmpty(priority))
            filters.Add(filterBuilder.Eq(t => t.Priority, priority));

        if (!string.IsNullOrEmpty(tag))
            filters.Add(filterBuilder.AnyEq(t => t.Tags, tag));

        if (!string.IsNullOrEmpty(search))
        {
            var searchFilter = filterBuilder.Or(
                filterBuilder.Regex(t => t.Title, new MongoDB.Bson.BsonRegularExpression(search, "i")),
                filterBuilder.Regex(t => t.Description, new MongoDB.Bson.BsonRegularExpression(search, "i"))
            );
            filters.Add(searchFilter);
        }

        var finalFilter = filters.Count > 0
            ? filterBuilder.And(filters)
            : filterBuilder.Empty;

        return await _tasks.Find(finalFilter).ToListAsync();
    }

    public async Task<TaskItem?> GetTaskByIdAsync(string id)
    {
        return await _tasks.Find(t => t.Id == id).FirstOrDefaultAsync();
    }

    public async Task CreateTaskAsync(TaskItem task)
    {
        await _tasks.InsertOneAsync(task);
    }

    public async Task<bool> UpdateTaskAsync(string id, TaskUpdate update)
    {
        var updateDef = Builders<TaskItem>.Update;
        var updates = new List<UpdateDefinition<TaskItem>>();

        if (!string.IsNullOrEmpty(update.Title))
            updates.Add(updateDef.Set(t => t.Title, update.Title));

        if (update.Description != null)
            updates.Add(updateDef.Set(t => t.Description, update.Description));

        if (!string.IsNullOrEmpty(update.Status))
            updates.Add(updateDef.Set(t => t.Status, update.Status));

        if (!string.IsNullOrEmpty(update.Priority))
            updates.Add(updateDef.Set(t => t.Priority, update.Priority));

        if (update.DueDate != null)
            updates.Add(updateDef.Set(t => t.DueDate, update.DueDate));

        if (update.Tags != null)
            updates.Add(updateDef.Set(t => t.Tags, update.Tags));

        if (update.AssignedUsers != null)
            updates.Add(updateDef.Set(t => t.AssignedUsers, update.AssignedUsers));

        if (updates.Count == 0)
            return false;

        var combined = updateDef.Combine(updates);
        var result = await _tasks.UpdateOneAsync(t => t.Id == id, combined);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteTaskAsync(string id)
    {
        var result = await _tasks.DeleteOneAsync(t => t.Id == id);
        return result.DeletedCount > 0;
    }

    public async Task<bool> DeleteTasksByUserIdAsync(string userId)
    {
        var result = await _tasks.DeleteManyAsync(t => t.UserId == userId);
        return result.DeletedCount > 0;
    }
    public async Task<bool> UpdateTaskAttachmentsAsync(string id, List<TaskAttachment> attachments)
    {
        var updateDef = Builders<TaskItem>.Update.Set(t => t.Attachments, attachments);
        var result = await _tasks.UpdateOneAsync(t => t.Id == id, updateDef);
        return result.ModifiedCount > 0;
    }

    public async Task UpdateTaskStatusAsync(string taskId, string newStatus)
    {
        var update = Builders<TaskItem>.Update.Set(t => t.Status, newStatus);
        await _tasks.UpdateOneAsync(t => t.Id == taskId, update);
    }
}