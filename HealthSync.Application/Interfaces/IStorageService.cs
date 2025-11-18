using Microsoft.AspNetCore.Http;

namespace HealthSync.Application.Interfaces;

public interface IStorageService
{
    /// <summary>
    /// Uploads a file to storage and returns the public URL (or object path).
    /// </summary>
    /// <param name="file">File to upload</param>
    /// <param name="folder">Logical folder/bucket path (e.g. "avatars")</param>
    /// <param name="fileName">Optional file name to use (if null a GUID will be used)</param>
    Task<string> UploadAsync(IFormFile file, string folder, string? fileName = null);

    /// <summary>
    /// Deletes an object from storage by object name or path.
    /// </summary>
    Task DeleteAsync(string objectName);
}
