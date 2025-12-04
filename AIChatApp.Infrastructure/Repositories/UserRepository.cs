using AIChatApp.Domain.Entities;
using AIChatApp.Domain.Interfaces;
using AIChatApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIChatApp.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByTokenAsync(string token)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.VerificationToken == token);
    }

    public async Task<User?> GetByResetTokenAsync(string resetToken)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == resetToken);
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.UserName == username);
    }

    public Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(User user)
    {
        _context.Users.Remove(user);
        return Task.CompletedTask;
    }

    public async Task<List<User>> GetRecentUsersAsync(DateTime cutoffDate, Guid excludedUserId)
    {
        return await _context.Users
            .Where(u => u.Id != excludedUserId && u.CreatedAt >= cutoffDate) // Added Filter
            .OrderByDescending(u => u.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<User>> GetRandomUsersAsync(int count, Guid excludedUserId)
    {
        // Fetch only IDs first for efficiency, then randomize in memory
        var userIds = await _context.Users
            .Where(u => u.Id != excludedUserId)
            .Select(u => u.Id)
            .ToListAsync();

        // Randomize using local random (much faster than DB-side GUID generation)
        var randomIds = userIds
            .OrderBy(_ => Random.Shared.Next())
            .Take(count)
            .ToList();

        // Fetch the actual user data
        return await _context.Users
            .Where(u => randomIds.Contains(u.Id))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<User>> SearchUsersAsync(string query, Guid excludedUserId)
    {
        var normalizedQuery = query.Trim();

        // SQL Server LIKE is case-insensitive by default, but explicit case-insensitive comparison is safer
        return await _context.Users
            .Where(u => u.Id != excludedUserId && 
                       (EF.Functions.Like(u.FullName, $"%{normalizedQuery}%", "\\") ||
                        EF.Functions.Like(u.UserName, $"%{normalizedQuery}%", "\\")))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}