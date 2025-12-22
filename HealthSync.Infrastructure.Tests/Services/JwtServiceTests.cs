using FluentAssertions;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace HealthSync.Infrastructure.Tests.Services;

public class JwtServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly JwtService _jwtService;

    public JwtServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "JwtSettings:SecretKey", "supersecretkeythatislongenoughforjwttesting123456789" },
            { "JwtSettings:Issuer", "HealthSyncAPI" },
            { "JwtSettings:Audience", "HealthSyncClient" },
            { "JwtSettings:AccessTokenExpirationMinutes", "15" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _jwtService = new JwtService(_configuration);
    }

    [Fact]
    public void GenerateAccessToken_ShouldCreateValidJwtToken()
    {
        // Arrange
        var user = new ApplicationUser
        {
            UserId = 1,
            Email = "test@example.com",
            Role = "Customer"
        };

        // Act
        var token = _jwtService.GenerateAccessToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        jwtToken.Should().NotBeNull();
        jwtToken.Issuer.Should().Be("HealthSyncAPI");
        jwtToken.Audiences.Should().Contain("HealthSyncClient");

        var claims = jwtToken.Claims.ToList();
        claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == "1");
        claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "test@example.com");
        claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Customer");
        claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateAccessToken_ShouldThrowException_WhenSecretKeyIsNotConfigured()
    {
        // Arrange
        var emptyConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var jwtService = new JwtService(emptyConfig);
        var user = new ApplicationUser
        {
            UserId = 1,
            Email = "test@example.com",
            Role = "Customer"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            jwtService.GenerateAccessToken(user));

        exception.Message.Should().Be("JWT Secret Key is not configured.");
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnBase64String()
    {
        // Act
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Assert
        refreshToken.Should().NotBeNullOrEmpty();

        // Should be valid base64
        var bytes = Convert.FromBase64String(refreshToken);
        bytes.Length.Should().Be(32); // 32 bytes = 256 bits
    }

    [Fact]
    public void GenerateRefreshToken_ShouldGenerateUniqueTokens()
    {
        // Act
        var token1 = _jwtService.GenerateRefreshToken();
        var token2 = _jwtService.GenerateRefreshToken();

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void GenerateAccessToken_ShouldHaveCorrectExpiration()
    {
        // Arrange
        var user = new ApplicationUser
        {
            UserId = 1,
            Email = "test@example.com",
            Role = "Customer"
        };

        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = _jwtService.GenerateAccessToken(user);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        jwtToken.ValidTo.Should().BeAfter(beforeGeneration.AddMinutes(14));
        jwtToken.ValidTo.Should().BeBefore(beforeGeneration.AddMinutes(16));
    }

    [Fact]
    public void GenerateAccessToken_ShouldIncludeAllRequiredClaims()
    {
        // Arrange
        var user = new ApplicationUser
        {
            UserId = 123,
            Email = "user@test.com",
            Role = "Admin"
        };

        // Act
        var token = _jwtService.GenerateAccessToken(user);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        var claims = jwtToken.Claims.ToDictionary(c => c.Type, c => c.Value);

        claims.Should().ContainKey(JwtRegisteredClaimNames.Sub);
        claims[JwtRegisteredClaimNames.Sub].Should().Be("123");

        claims.Should().ContainKey(JwtRegisteredClaimNames.Email);
        claims[JwtRegisteredClaimNames.Email].Should().Be("user@test.com");

        claims.Should().ContainKey(ClaimTypes.Role);
        claims[ClaimTypes.Role].Should().Be("Admin");

        claims.Should().ContainKey(JwtRegisteredClaimNames.Jti);
        Guid.TryParse(claims[JwtRegisteredClaimNames.Jti], out _).Should().BeTrue();
    }
}

