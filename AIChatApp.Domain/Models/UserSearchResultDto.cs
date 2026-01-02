namespace AIChatApp.Domain.Models;

public class UserSearchResultDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public bool HasChatted { get; set; } // The logic flag
}