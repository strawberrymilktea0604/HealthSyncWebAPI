using HealthSync.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.Exceptions;
using System.IO;
using System.Threading.Tasks;

namespace HealthSync.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly MinioClient _client;
    private readonly string _bucket;
    private readonly bool _useSsl;
    private readonly string _endpoint;

    public FileStorageService(IConfiguration configuration)
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

    public async Task<string> UploadAsync(IFormFile file, string folder)
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
        var name = $"{Guid.NewGuid()}{ext}";
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

    public async Task DeleteFileAsync(string fileUrl)
    {
        if (string.IsNullOrEmpty(fileUrl)) return;

        try
        {
            // Extract object name from URL (assuming format: scheme://endpoint/bucket/objectName)
            var uri = new Uri(fileUrl);
            var pathParts = uri.AbsolutePath.TrimStart('/').Split('/');
            if (pathParts.Length < 2) return; // Invalid URL format

            var objectName = string.Join("/", pathParts.Skip(1)); // Skip bucket name
            await _client.RemoveObjectAsync(new RemoveObjectArgs().WithBucket(_bucket).WithObject(objectName));
        }
        catch (MinioException)
        {
            // ignore not found / deletion errors for idempotency
        }
    }

    public async Task<bool> FileExistsAsync(string fileUrl)
    {
        try
        {
            // Extract object name from URL (assuming format: scheme://endpoint/bucket/objectName)
            var uri = new Uri(fileUrl);
            var pathParts = uri.AbsolutePath.TrimStart('/').Split('/');
            if (pathParts.Length < 2) return false; // Invalid URL format

            var objectName = string.Join("/", pathParts.Skip(1)); // Skip bucket name
            await _client.StatObjectAsync(new StatObjectArgs().WithBucket(_bucket).WithObject(objectName));
            return true;
        }
        catch (MinioException)
        {
            return false;
        }
    }
}