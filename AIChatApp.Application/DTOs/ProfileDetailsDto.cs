namespace AIChatApp.Application.DTOs;

public class ProfileDetailsDto
{
    public string? ProfileImageUrl { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RegisteredDate { get; set; } = string.Empty; // Formatted date
    public bool IsVerified { get; set; }
}