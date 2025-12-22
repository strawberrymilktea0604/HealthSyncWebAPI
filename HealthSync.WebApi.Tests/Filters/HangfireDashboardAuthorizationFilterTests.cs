using Hangfire.Dashboard;
using HealthSync.WebApi.Filters;
using System.Security.Claims;
using Xunit;

namespace HealthSync.WebApi.Tests.Filters;

// Testable subclass that overrides GetUser
internal class TestableHangfireDashboardAuthorizationFilter : HangfireDashboardAuthorizationFilter
{
    private readonly ClaimsPrincipal _user;

    public TestableHangfireDashboardAuthorizationFilter(ClaimsPrincipal user)
    {
        _user = user;
    }

    protected override ClaimsPrincipal GetUser(DashboardContext context)
    {
        return _user;
    }
}

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

    [Fact]
    public void AuthorizeUser_ShouldReturnFalse_WhenUserHasNoRoleClaim()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "2"),
            new Claim(ClaimTypes.Name, "user@test.com")
            // No Role claim
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = HangfireDashboardAuthorizationFilter.AuthorizeUser(principal);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AuthorizeUser_ShouldReturnFalse_WhenIdentityIsNull()
    {
        // Arrange
        var principal = new ClaimsPrincipal(); // No identity

        // Act
        var result = HangfireDashboardAuthorizationFilter.AuthorizeUser(principal);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AuthorizeUser_ShouldBeCaseSensitive_ForRoleCheck()
    {
        // Arrange - lowercase "admin"
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "3"),
            new Claim(ClaimTypes.Role, "admin") // lowercase
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = HangfireDashboardAuthorizationFilter.AuthorizeUser(principal);

        // Assert
        Assert.False(result); // Case-sensitive: "Admin" != "admin"
    }

    [Fact]
    public void AuthorizeUser_ShouldReturnFalse_WhenRoleIsEmpty()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "4"),
            new Claim(ClaimTypes.Role, "") // Empty role
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = HangfireDashboardAuthorizationFilter.AuthorizeUser(principal);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AuthorizeUser_ShouldReturnFalse_WhenAuthenticationTypeIsNull()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "5"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims); // No authentication type = not authenticated
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = HangfireDashboardAuthorizationFilter.AuthorizeUser(principal);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AuthorizeUser_ShouldReturnFalse_WhenAuthenticationTypeIsEmpty()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "6"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, string.Empty); // Empty authentication type
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = HangfireDashboardAuthorizationFilter.AuthorizeUser(principal);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("SuperAdmin")]
    [InlineData("Administrator")]
    [InlineData("ADMIN")]
    [InlineData("AdminRole")]
    [InlineData("Admin ")]
    [InlineData(" Admin")]
    public void AuthorizeUser_ShouldReturnFalse_ForSimilarButNotExactAdminRoles(string role)
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "7"),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = HangfireDashboardAuthorizationFilter.AuthorizeUser(principal);

        // Assert
        Assert.False(result); // Only exact "Admin" should pass
    }

    [Fact]
    public void AuthorizeUser_ShouldReturnFalse_WhenUserHasMultipleRolesButNotAdmin()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "8"),
            new Claim(ClaimTypes.Role, "Customer"),
            new Claim(ClaimTypes.Role, "Moderator")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = HangfireDashboardAuthorizationFilter.AuthorizeUser(principal);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AuthorizeUser_ShouldReturnTrue_WhenAdminRoleIsFirstAmongMultiple()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "9"),
            new Claim(ClaimTypes.Role, "Admin"), // Admin first
            new Claim(ClaimTypes.Role, "Customer")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = HangfireDashboardAuthorizationFilter.AuthorizeUser(principal);

        // Assert
        Assert.True(result); // FindFirst returns "Admin"
    }

    [Fact]
    public void AuthorizeUser_ShouldReturnTrue_WithCompleteAdminUserProfile()
    {
        // Arrange - Full realistic admin claims
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "10"),
            new Claim(ClaimTypes.Email, "admin@healthsync.com"),
            new Claim(ClaimTypes.Name, "Admin User"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("custom_claim", "some_value")
        };
        var identity = new ClaimsIdentity(claims, "JWT");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = HangfireDashboardAuthorizationFilter.AuthorizeUser(principal);

        // Assert
        Assert.True(result);
    }

    #region Authorize(DashboardContext) Instance Method Tests

    [Fact]
    public void Authorize_InstanceMethod_ShouldReturnFalse_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var identity = new ClaimsIdentity(); // Not authenticated
        var principal = new ClaimsPrincipal(identity);
        var filter = new TestableHangfireDashboardAuthorizationFilter(principal);

        // Act - DashboardContext can be null since we override GetUser
        var result = filter.Authorize(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Authorize_InstanceMethod_ShouldReturnTrue_WhenUserIsAuthenticatedAdmin()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "JWT");
        var principal = new ClaimsPrincipal(identity);
        var filter = new TestableHangfireDashboardAuthorizationFilter(principal);

        // Act
        var result = filter.Authorize(null!);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Authorize_InstanceMethod_ShouldReturnFalse_WhenUserIsCustomer()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "2"),
            new Claim(ClaimTypes.Role, "Customer")
        };
        var identity = new ClaimsIdentity(claims, "JWT");
        var principal = new ClaimsPrincipal(identity);
        var filter = new TestableHangfireDashboardAuthorizationFilter(principal);

        // Act
        var result = filter.Authorize(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Authorize_InstanceMethod_ShouldReturnFalse_WhenUserHasNoRoleClaim()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "3"),
            new Claim(ClaimTypes.Email, "user@test.com")
        };
        var identity = new ClaimsIdentity(claims, "JWT");
        var principal = new ClaimsPrincipal(identity);
        var filter = new TestableHangfireDashboardAuthorizationFilter(principal);

        // Act
        var result = filter.Authorize(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Authorize_InstanceMethod_ShouldReturnFalse_ForMultipleInvalidScenarios()
    {
        // Test: User with null identity
        var filter1 = new TestableHangfireDashboardAuthorizationFilter(new ClaimsPrincipal());
        Assert.False(filter1.Authorize(null!));

        // Test: Empty role string
        var claims2 = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "4"),
            new Claim(ClaimTypes.Role, "")
        };
        var filter2 = new TestableHangfireDashboardAuthorizationFilter(
            new ClaimsPrincipal(new ClaimsIdentity(claims2, "JWT")));
        Assert.False(filter2.Authorize(null!));

        // Test: Wrong role casing
        var claims3 = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "5"),
            new Claim(ClaimTypes.Role, "admin") // lowercase
        };
        var filter3 = new TestableHangfireDashboardAuthorizationFilter(
            new ClaimsPrincipal(new ClaimsIdentity(claims3, "JWT")));
        Assert.False(filter3.Authorize(null!));
    }

    #endregion
}
