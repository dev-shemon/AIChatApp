using Microsoft.AspNetCore.Http; // Required for IFormFile
using System.Threading.Tasks;

namespace AIChatApp.Domain.Interfaces;

public interface IFileStorageService
{
    // Changed to Accept IFormFile to handle extensions/types easily in the implementation
    Task<string> SaveFileAsync(IFormFile file);

    Task DeleteFileAsync(string fileUrl);
}