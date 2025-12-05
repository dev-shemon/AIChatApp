using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace AIChatApp.Application.DTOs;

public class UpdateProfilePictureDto
{
    // The user ID is required to know which user's profile to update
    [Required]
    public Guid UserId { get; set; }

    // Store the current profile picture URL to display in the form
    public string? CurrentProfilePictureUrl { get; set; }

    // This property will hold the uploaded file from the web form
    [Required(ErrorMessage = "Please select a file to upload.")]
    [DataType(DataType.Upload)]
    public IFormFile ProfileImageFile { get; set; } = null!;
}