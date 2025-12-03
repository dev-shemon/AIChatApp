using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace AIChatApp.Application.DTOs;

public class UpdateProfilePictureDto
{
    // The user ID is required to know which user's profile to update.
    [Required]
    public Guid UserId { get; set; }

    // This property will hold the uploaded file from the web form.
    [Required(ErrorMessage = "Please select a file to upload.")]
    [DataType(DataType.Upload)]
    // Optional: Add custom validation attributes here (e.g., file size, accepted extensions)
    public IFormFile ProfileImageFile { get; set; } = null!;
}