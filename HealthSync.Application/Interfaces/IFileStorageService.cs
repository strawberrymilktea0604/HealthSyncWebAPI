using Microsoft.AspNetCore.Http;

namespace HealthSync.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadAsync(IFormFile file, string folder);
    Task DeleteFileAsync(string fileUrl);
    Task<bool> FileExistsAsync(string fileUrl);
}