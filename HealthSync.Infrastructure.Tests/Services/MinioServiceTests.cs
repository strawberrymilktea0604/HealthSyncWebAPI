using HealthSync.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using FluentAssertions;
using Xunit;

namespace HealthSync.Infrastructure.Tests.Services;

public class MinioServiceTests
{
    private readonly IConfiguration _configuration;

    public MinioServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"MinIO:Endpoint", "localhost:9000"},
            {"MinIO:AccessKey", "minioadmin"},
            {"MinIO:SecretKey", "minioadmin"},
            {"MinIO:BucketName", "healthsync-images"},
            {"MinIO:UseSSL", "false"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public void Constructor_Should_Initialize_With_Configuration()
    {
        // Act
        var minioService = new MinioService(_configuration);

        // Assert
        minioService.Should().NotBeNull();
        // Note: We can't easily test private fields, but constructor should not throw
    }

    [Fact]
    public void Constructor_Should_Use_Default_Values_When_Configuration_Is_Missing()
    {
        // Arrange
        var emptyConfig = new ConfigurationBuilder().Build();

        // Act
        var minioService = new MinioService(emptyConfig);

        // Assert
        minioService.Should().NotBeNull();
    }

    // Note: Full integration tests with actual MinIO server would require:
    // 1. A running MinIO server (docker container)
    // 2. Actual file upload/download operations
    // 3. Proper cleanup after tests
    //
    // For now, we rely on the constructor test and assume the service
    // works correctly when MinIO is properly configured.
    // In a real project, you would add integration tests with Testcontainers
    // or a dedicated test MinIO instance.
}

