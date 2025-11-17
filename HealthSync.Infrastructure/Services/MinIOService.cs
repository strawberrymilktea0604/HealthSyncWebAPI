using Amazon.S3;
using Amazon.S3.Model;
using HealthSync.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace HealthSync.Infrastructure.Services;

public class MinIOService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _endpoint;

    public MinIOService(IConfiguration configuration)
    {
        var minIOConfig = configuration.GetSection("MinIO");
        var accessKey = minIOConfig["AccessKey"];
        var secretKey = minIOConfig["SecretKey"];
        _bucketName = minIOConfig["BucketName"] ?? "healthsync-images";
        _endpoint = minIOConfig["Endpoint"] ?? "localhost:9000";
        var useSSL = bool.Parse(minIOConfig["UseSSL"] ?? "false");

        var config = new AmazonS3Config
        {
            ServiceURL = useSSL ? $"https://{_endpoint}" : $"http://{_endpoint}",
            ForcePathStyle = true, // Required for MinIO
            UseHttp = !useSSL
        };

        _s3Client = new AmazonS3Client(accessKey, secretKey, config);

        // Ensure bucket exists
        EnsureBucketExistsAsync().GetAwaiter().GetResult();
    }

    private async Task EnsureBucketExistsAsync()
    {
        try
        {
            var listBucketsResponse = await _s3Client.ListBucketsAsync();
            if (!listBucketsResponse.Buckets.Any(b => b.BucketName == _bucketName))
            {
                var putBucketRequest = new PutBucketRequest
                {
                    BucketName = _bucketName,
                    UseClientRegion = true
                };
                await _s3Client.PutBucketAsync(putBucketRequest);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't throw - bucket creation might fail in some environments
            Console.WriteLine($"Error ensuring bucket exists: {ex.Message}");
        }
    }

    public async Task<string> UploadAsync(IFormFile file, string folder)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is required");

        // Generate unique filename with timestamp
        var fileName = $"{Guid.NewGuid()}_{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}";
        var key = $"{folder}/{fileName}";

        using var stream = file.OpenReadStream();

        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream,
            ContentType = file.ContentType,
            AutoCloseStream = false
        };

        var response = await _s3Client.PutObjectAsync(putRequest);

        // Return the file URL
        var useSSL = _endpoint.Contains("https");
        return $"{(useSSL ? "https" : "http")}://{_endpoint}/{_bucketName}/{key}";
    }

    public async Task DeleteFileAsync(string fileUrl)
    {
        // Extract key from URL
        var uri = new Uri(fileUrl);
        var key = uri.AbsolutePath.TrimStart('/').Replace($"{_bucketName}/", "");

        var deleteRequest = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        };

        await _s3Client.DeleteObjectAsync(deleteRequest);
    }

    public async Task<bool> FileExistsAsync(string fileUrl)
    {
        try
        {
            // Extract key from URL
            var uri = new Uri(fileUrl);
            var key = uri.AbsolutePath.TrimStart('/').Replace($"{_bucketName}/", "");

            var request = new GetObjectMetadataRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            await _s3Client.GetObjectMetadataAsync(request);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}