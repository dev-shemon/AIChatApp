using System.ComponentModel.DataAnnotations;

namespace AIChatApp.Application.DTOs;

public class EditProfileDto
{
    // We need the User ID to know which record to update in the database.
    [Required]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Full Name is required.")]
    [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers, and underscores.")]
    public string UserName { get; set; } = string.Empty;

    // Read-only fields displayed for context but not edited in this form
    public string CurrentEmail { get; set; } = string.Empty;
}