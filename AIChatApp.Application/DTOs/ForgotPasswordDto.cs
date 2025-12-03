using System.ComponentModel.DataAnnotations;

namespace AIChatApp.Application.DTOs;

public class ForgotPasswordDto
{
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;
}