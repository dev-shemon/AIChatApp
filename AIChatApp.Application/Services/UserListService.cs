using AIChatApp.Application.Interfaces;
using AIChatApp.Domain.DTOs;
using AIChatApp.Domain.Entities;
using AIChatApp.Domain.Interfaces;

namespace AIChatApp.Application.Services;

public class UserListService : IUserListService
{
    private readonly IUserRepository _userRepository;

    public UserListService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserListViewModel> GetDashboardAsync(Guid currentUserId)
    {
        var fiveDaysAgo = DateTime.UtcNow.AddDays(-5);

        // Pass the ID to the repository
        var recentUsers = await _userRepository.GetRecentUsersAsync(fiveDaysAgo, currentUserId);
        var randomUsers = await _userRepository.GetRandomUsersAsync(5, currentUserId);

        return new UserListViewModel
        {
            RecentUsers = recentUsers.Select(MapToDto).ToList(),
            RandomUsers = randomUsers.Select(MapToDto).ToList()
        };
    }

    public async Task<List<UserListDto>> SearchUsersAsync(string query, Guid currentUserId)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<UserListDto>();

        // Pass the ID to the repository
        var users = await _userRepository.SearchUsersAsync(query, currentUserId);
        return users.Select(MapToDto).ToList();
    }

    // Helper to map Entity to DTO
    private static UserListDto MapToDto(User user)
    {
        return new UserListDto
        {
            Id = user.Id.ToString(),
            FullName = user.FullName,
            UserName = user.UserName,
            ProfileImageUrl = user.ProfileImageUrl,
            CreatedAt = user.CreatedAt
        };
    }
}