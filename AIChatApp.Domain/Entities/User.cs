namespace AIChatApp.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; // Never store plain text!
    public string? ProfileImageUrl { get; set; }

    // Verification Logic
    public string? VerificationToken { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public bool IsVerified => VerifiedAt.HasValue;

    public string? PasswordResetToken { get; set; } 
    public DateTime? ResetTokenExpires { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}