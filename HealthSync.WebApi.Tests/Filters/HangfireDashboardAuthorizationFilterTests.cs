using Hangfire.Dashboard;
using HealthSync.WebApi.Filters;
using System.Security.Claims;
using Xunit;

namespace HealthSync.WebApi.Tests.Filters;

public class HangfireDashboardAuthorizationFilterTests
{
    [Fact]
    public void Authorize_ShouldReturnFalse_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = HangfireDashboardAuthorizationFilter.AuthorizeUser(principal);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Authorize_ShouldReturnFalse_WhenUserIsAuthenticatedButNotAdmin()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Customer")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = HangfireDashboardAuthorizationFilter.AuthorizeUser(principal);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Authorize_ShouldReturnTrue_WhenUserIsAuthenticatedAndAdmin()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = HangfireDashboardAuthorizationFilter.AuthorizeUser(principal);

        // Assert
        Assert.True(result);
    }
}

