using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.Exceptions;

namespace HealthSync.Infrastructure.Data.Seeding;

/// <summary>
/// Handles image seeding to MinIO storage.
/// Provides idempotent upload (skip if exists).
/// </summary>
public class ImageSeeder
{
    private readonly MinioClient _minioClient;
    private readonly string _bucket;
    private readonly string _endpoint;
    private readonly bool _useSsl;
    private readonly string _seedImagePath;
    private readonly ILogger<ImageSeeder> _logger;

    public ImageSeeder(
        MinioClient minioClient,
        string bucket,
        string endpoint,
        bool useSsl,
        string seedImagePath,
        ILogger<ImageSeeder> logger)
    {
        _minioClient = minioClient;
        _bucket = bucket;
        _endpoint = endpoint;
        _useSsl = useSsl;
        _seedImagePath = seedImagePath;
        _logger = logger;
    }

    /// <summary>
    /// Ensures a seed image exists in MinIO bucket.
    /// Returns the public URL if uploaded, null if file not found locally.
    /// </summary>
    public async Task<string?> EnsureImageAsync(
        string localFileName,
        string folder,
        CancellationToken cancellationToken = default)
    {
        var objectName = $"{folder.Trim('/')}/{localFileName}";
        var localFilePath = Path.Combine(_seedImagePath, folder, localFileName);

        // Check if local file exists
        if (!File.Exists(localFilePath))
        {
            _logger.LogWarning("Seed image not found: {FilePath}", localFilePath);
            return null;
        }

        try
        {
            // Ensure bucket exists
            await EnsureBucketExistsAsync(cancellationToken);

            // Check if object already exists (idempotent)
            if (await ObjectExistsAsync(objectName, cancellationToken))
            {
                _logger.LogDebug("Image already exists in MinIO: {ObjectName}", objectName);
                return BuildPublicUrl(objectName);
            }

            // Upload new image
            await using var stream = File.OpenRead(localFilePath);
            var contentType = GetContentType(localFileName);

            var args = new PutObjectArgs()
                .WithBucket(_bucket)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(contentType);

            await _minioClient.PutObjectAsync(args, cancellationToken);

            _logger.LogInformation("Uploaded seed image: {ObjectName}", objectName);
            return BuildPublicUrl(objectName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload seed image: {FileName}", localFileName);
            return null;
        }
    }

    private async Task EnsureBucketExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var found = await _minioClient.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_bucket),
                cancellationToken);

            if (!found)
            {
                await _minioClient.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(_bucket),
                    cancellationToken);

                _logger.LogInformation("Created MinIO bucket: {Bucket}", _bucket);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to ensure bucket exists: {Bucket}", _bucket);
        }
    }

    private async Task<bool> ObjectExistsAsync(string objectName, CancellationToken cancellationToken)
    {
        try
        {
            await _minioClient.StatObjectAsync(
                new StatObjectArgs().WithBucket(_bucket).WithObject(objectName),
                cancellationToken);
            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    private string BuildPublicUrl(string objectName)
    {
        var scheme = _useSsl ? "https" : "http";
        return $"{scheme}://{_endpoint}/{_bucket}/{objectName}";
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }
}
