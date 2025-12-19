using AIChatApp.Domain.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AIChatApp.Infrastructure.Repositories;

public class CloudinaryStorageService : IFileStorageService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryStorageService(IConfiguration config)
    {
        var account = new Account(
            config["CloudinarySettings:CloudName"],
            config["CloudinarySettings:ApiKey"],
            config["CloudinarySettings:ApiSecret"]
        );
        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> SaveFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0) return null;

        using var stream = file.OpenReadStream();
        var fileName = file.FileName;
        var extension = Path.GetExtension(fileName).ToLower();

        // 1. Check if Image
        var imageExtensions = new List<string> { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

        // Create a variable to hold the result
        RawUploadResult result;

        if (imageExtensions.Contains(extension))
        {
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(fileName, stream),
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };
            result = await _cloudinary.UploadAsync(uploadParams);
        }
        else
        {
            // Handle PDF/Raw
            var uploadParams = new RawUploadParams()
            {
                File = new FileDescription(fileName, stream)
            };
            result = await _cloudinary.UploadAsync(uploadParams);
        }

        if (result.Error != null)
        {
            // This will print the ACTUAL error from Cloudinary to your Debug Console
            System.Diagnostics.Debug.WriteLine($"Cloudinary Error: {result.Error.Message}");
            throw new Exception($"Cloudinary Upload Failed: {result.Error.Message}");
        }

        if (result.SecureUrl == null)
        {
            throw new Exception("Cloudinary upload succeeded but returned no URL.");
        }

        return result.SecureUrl.ToString();
    }

    // Implement Delete if your Interface requires it
    public async Task DeleteFileAsync(string fileUrl)
    {
        // Optional: Logic to delete from cloud
        await Task.CompletedTask;
    }
}