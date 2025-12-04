using AIChatApp.Domain.DTOs;

namespace AIChatApp.Application.Interfaces;

public interface IUserListService
{
    Task<UserListViewModel> GetDashboardAsync(Guid currentUserId);
    Task<List<UserListDto>> SearchUsersAsync(string query, Guid currentUserId);
}

// We define a simple view model here for the dashboard structure
public class UserListViewModel
{
    public List<UserListDto> RecentUsers { get; set; } = new();
    public List<UserListDto> RandomUsers { get; set; } = new();
}