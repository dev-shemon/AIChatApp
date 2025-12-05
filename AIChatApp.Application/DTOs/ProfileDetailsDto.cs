namespace AIChatApp.Application.DTOs;

public class ProfileDetailsDto
{
    public string? ProfileImageUrl { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime RegisteredDate { get; set; } // Formatted date
    public bool IsVerified { get; set; }
}