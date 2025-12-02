using System.ComponentModel.DataAnnotations;

namespace AIChatApp.Application.DTOs;

public class DeleteAccountDto
{
    // Hidden field to identify the user (populated by the controller)
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Please enter your current password to confirm deletion.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}