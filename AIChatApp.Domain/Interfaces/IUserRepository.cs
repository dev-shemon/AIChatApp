using AIChatApp.Domain.Entities;

namespace AIChatApp.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByTokenAsync(string token);
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByUsernameAsync(string username);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(User user);
    Task<List<User>> GetRecentUsersAsync(DateTime cutoffDate, Guid excludedUserId);
    Task<List<User>> GetRandomUsersAsync(int count, Guid excludedUserId);
    Task<List<User>> SearchUsersAsync(string query, Guid excludedUserId);
    Task SaveChangesAsync(); // Commit changes
}