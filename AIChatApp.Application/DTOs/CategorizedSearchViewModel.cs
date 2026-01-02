using AIChatApp.Domain.DTOs;

namespace AIChatApp.Application.DTOs;

public class CategorizedSearchViewModel
{
    public List<UserListDto> ChattedUsers { get; set; } = new();
    public List<UserListDto> MorePeople { get; set; } = new();
    public string SearchQuery { get; set; } = string.Empty;
}