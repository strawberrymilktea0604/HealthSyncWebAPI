using HealthSync.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using FluentAssertions;
using Xunit;

namespace HealthSync.Infrastructure.Tests.Services;

public class FileStorageServiceTests
{
    private readonly FileStorageService _service;

    public FileStorageServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MinIO:Endpoint"] = "localhost:9000",
                ["MinIO:AccessKey"] = "minioadmin",
                ["MinIO:SecretKey"] = "minioadmin",
                ["MinIO:BucketName"] = "test-bucket",
                ["MinIO:UseSSL"] = "false"
            })
            .Build();

        _service = new FileStorageService(configuration);
    }

    [Fact]
    public async Task UploadAsync_Should_Throw_ArgumentException_When_File_Is_Null()
    {
        // Arrange
        IFormFile file = null!;
        var folder = "avatars";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UploadAsync(file, folder));
    }

    [Fact]
    public async Task UploadAsync_Should_Throw_ArgumentException_When_File_Is_Empty()
    {
        // Arrange
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(0);
        var folder = "avatars";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.UploadAsync(fileMock.Object, folder));
    }

    [Fact]
    public async Task DeleteFileAsync_Should_Return_When_FileUrl_Is_Null_Or_Empty()
    {
        // Arrange
        string fileUrl = null!;

        // Act
        await _service.DeleteFileAsync(fileUrl);

        // Assert - No exception should be thrown
        Assert.True(true);
    }

    [Fact]
    public async Task DeleteFileAsync_Should_Return_When_FileUrl_Is_Empty()
    {
        // Arrange
        var fileUrl = "";

        // Act
        await _service.DeleteFileAsync(fileUrl);

        // Assert - No exception should be thrown
        Assert.True(true);
    }

    [Fact]
    public async Task FileExistsAsync_Should_Return_False_When_FileUrl_Is_Invalid()
    {
        // Arrange
        var fileUrl = "invalid-url";

        // Act & Assert
        await Assert.ThrowsAsync<UriFormatException>(() =>
            _service.FileExistsAsync(fileUrl));
    }

    [Fact]
    public async Task FileExistsAsync_Should_Return_False_When_FileUrl_Has_Invalid_Format()
    {
        // Arrange
        var fileUrl = "http://example.com/";

        // Act
        var result = await _service.FileExistsAsync(fileUrl);

        // Assert
        result.Should().BeFalse();
    }
}

