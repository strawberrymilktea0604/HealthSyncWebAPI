using FluentAssertions;
using HealthSync.Domain.Entities;
using Xunit;

namespace HealthSync.Domain.Tests.Entities;

public class ApplicationUserTests
{
    [Fact]
    public void ApplicationUser_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var user = new ApplicationUser();

        // Assert
        user.UserId.Should().Be(0);
        user.Email.Should().BeNull();
        user.PasswordHash.Should().BeNull();
        user.Role.Should().BeNull();
        user.IsActive.Should().BeTrue(); // Default value
        user.OauthProvider.Should().BeNull();
        user.OauthProviderId.Should().BeNull();
        user.RefreshToken.Should().BeNull();
        user.RefreshTokenExpiry.Should().BeNull();
        user.CreatedAt.Should().Be(default(DateTime));
        user.LastLoginAt.Should().BeNull();
    }

    [Fact]
    public void ApplicationUser_ShouldAllowPropertyAssignment()
    {
        // Arrange
        var user = new ApplicationUser();
        var createdAt = DateTime.UtcNow;
        var lastLoginAt = DateTime.UtcNow.AddHours(-1);
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        // Act
        user.UserId = 1;
        user.Email = "test@example.com";
        user.PasswordHash = "hashedpassword";
        user.Role = "Customer";
        user.IsActive = true;
        user.OauthProvider = "Google";
        user.OauthProviderId = "google123";
        user.RefreshToken = "refresh_token";
        user.RefreshTokenExpiry = refreshTokenExpiry;
        user.CreatedAt = createdAt;
        user.LastLoginAt = lastLoginAt;

        // Assert
        user.UserId.Should().Be(1);
        user.Email.Should().Be("test@example.com");
        user.PasswordHash.Should().Be("hashedpassword");
        user.Role.Should().Be("Customer");
        user.IsActive.Should().BeTrue();
        user.OauthProvider.Should().Be("Google");
        user.OauthProviderId.Should().Be("google123");
        user.RefreshToken.Should().Be("refresh_token");
        user.RefreshTokenExpiry.Should().Be(refreshTokenExpiry);
        user.CreatedAt.Should().Be(createdAt);
        user.LastLoginAt.Should().Be(lastLoginAt);
    }
}