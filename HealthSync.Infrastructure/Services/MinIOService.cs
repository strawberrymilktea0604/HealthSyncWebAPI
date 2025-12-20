using HealthSync.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.Exceptions;

namespace HealthSync.Infrastructure.Services;

public class MinioService : IStorageService, IFileStorageService
{
    private readonly MinioClient _client;
    private readonly string _bucket;
    private readonly bool _useSsl;
    private readonly string _endpoint;

    public MinioService(IConfiguration configuration)
    {
        _endpoint = configuration["MinIO:Endpoint"] ?? "localhost:9000";
        var accessKey = configuration["MinIO:AccessKey"] ?? "minioadmin";
        var secretKey = configuration["MinIO:SecretKey"] ?? "minioadmin";
        _bucket = configuration["MinIO:BucketName"] ?? "healthsync-images";
        bool.TryParse(configuration["MinIO:UseSSL"], out _useSsl);

        // Minio client accepts endpoint without scheme
        _client = new MinioClient()
            .WithEndpoint(_endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(_useSsl)
            .Build();
    }

    public async Task<string> UploadAsync(IFormFile file, string folder, string? fileName = null)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty", nameof(file));

        // Ensure bucket exists
        try
        {
            var found = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucket));
            if (!found)
            {
                await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucket));
            }
        }
        catch (Exception)
        {
            // swallow and continue - errors will surface on upload
        }

        var ext = Path.GetExtension(file.FileName);
        var name = string.IsNullOrEmpty(fileName) ? $"{Guid.NewGuid()}{ext}" : fileName;
        var objectName = string.IsNullOrEmpty(folder) ? name : $"{folder.Trim('/')}/{name}";

        using var stream = file.OpenReadStream();
        var args = new PutObjectArgs()
            .WithBucket(_bucket)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType(file.ContentType ?? "application/octet-stream");

        await _client.PutObjectAsync(args);

        // Build a simple URL (may need adjustment depending on MinIO reverse proxy)
        var scheme = _useSsl ? "https" : "http";
        var url = $"{scheme}://{_endpoint}/{_bucket}/{objectName}";
        return url;
    }

    public async Task DeleteAsync(string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return;

        try
        {
            await _client.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(_bucket).WithObject(objectName));
        }
        catch (MinioException)
        {
            // ignore not found / deletion errors for idempotency
        }
    }

    // IFileStorageService implementation
    public async Task<string> UploadAsync(IFormFile file, string folder)
    {
        return await UploadAsync(file, folder, null);
    }

    public async Task DeleteFileAsync(string fileUrl)
    {
        // Extract object name from URL
        // Format: http://endpoint/bucket/folder/filename
        if (string.IsNullOrEmpty(fileUrl)) return;

        try
        {
            var uri = new Uri(fileUrl);
            var pathParts = uri.AbsolutePath.TrimStart('/').Split('/', 2);
            if (pathParts.Length > 1)
            {
                var objectName = pathParts[1]; // Skip bucket name
                await DeleteAsync(objectName);
            }
        }
        catch
        {
            // If URL parsing fails, try direct deletion
            await DeleteAsync(fileUrl);
        }
    }

    public async Task<bool> FileExistsAsync(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return false;

        try
        {
            var uri = new Uri(fileUrl);
            var pathParts = uri.AbsolutePath.TrimStart('/').Split('/', 2);
            if (pathParts.Length > 1)
            {
                var objectName = pathParts[1];
                await _client.StatObjectAsync(new StatObjectArgs().WithBucket(_bucket).WithObject(objectName));
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }
}