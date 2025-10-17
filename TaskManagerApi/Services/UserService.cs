using MongoDB.Driver;
using TaskManagerApi.Models;

namespace TaskManagerApi.Services;

public class UserService
{
    private readonly IMongoCollection<User> _users;

    public UserService(IMongoDatabase database)
    {
        _users = database.GetCollection<User>("users");
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
    }

    public async Task<User?> GetUserByIdAsync(string id)
    {
        return await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        var users = await _users.Find(_ => true).ToListAsync();
        users.ForEach(u => u.HashedPassword = null);
        return users;
    }

    public async Task CreateUserAsync(User user)
    {
        await _users.InsertOneAsync(user);
    }

    public async Task<bool> UpdateUserAsync(string id, UserUpdate update)
    {
        var updateDef = Builders<User>.Update;
        var updates = new List<UpdateDefinition<User>>();

        if (!string.IsNullOrEmpty(update.Name))
            updates.Add(updateDef.Set(u => u.Name, update.Name));

        if (!string.IsNullOrEmpty(update.Email))
            updates.Add(updateDef.Set(u => u.Email, update.Email));

        if (!string.IsNullOrEmpty(update.Role))
            updates.Add(updateDef.Set(u => u.Role, update.Role));

        if (updates.Count == 0)
            return false;

        var combined = updateDef.Combine(updates);
        var result = await _users.UpdateOneAsync(u => u.Id == id, combined);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> DeleteUserAsync(string id)
    {
        var result = await _users.DeleteOneAsync(u => u.Id == id);
        return result.DeletedCount > 0;
    }
}