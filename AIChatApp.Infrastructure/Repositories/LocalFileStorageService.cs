using AIChatApp.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AIChatApp.Infrastructure.Repositories;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storagePath;
    private readonly string _baseUrl;

    public LocalFileStorageService(IConfiguration configuration)
    {
        // Path should be configured in appsettings (e.g., "FileStorage:StoragePath": "wwwroot/uploads/profiles")
        _storagePath = configuration["FileStorage:StoragePath"]
                       ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
        // Base URL for serving the images (e.g., "FileStorage:BaseUrl": "/uploads/profiles/")
        _baseUrl = configuration["FileStorage:BaseUrl"] ?? "/uploads/profiles/";

        // Ensure the directory exists
        Directory.CreateDirectory(_storagePath);
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        // Create a unique file name to prevent conflicts
        string uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        string filePath = Path.Combine(_storagePath, uniqueFileName);

        using (var fileOnlyStream = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fileOnlyStream);
        }

        // Return the URL path to the file
        return Path.Combine(_baseUrl, uniqueFileName).Replace('\\', '/');
    }

    public Task DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return Task.CompletedTask;

        // Extract the file name from the URL
        string fileName = Path.GetFileName(fileUrl.Replace('\\', '/'));
        string filePath = Path.Combine(_storagePath, fileName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }
}