using HealthSync.Application.DTOs.Auth;
using HealthSync.Application.Features.Auth.Interfaces;
using HealthSync.Application.Features.Auth.Services;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Moq;
using FluentAssertions;
using Xunit;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace HealthSync.Application.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUserProfileRepository> _userProfileRepositoryMock;
    private readonly Mock<ILeaderboardRepository> _leaderboardRepositoryMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _userProfileRepositoryMock = new Mock<IUserProfileRepository>();
        _leaderboardRepositoryMock = new Mock<ILeaderboardRepository>();
        _jwtServiceMock = new Mock<IJwtService>();
        _configurationMock = new Mock<IConfiguration>();

        _service = new AuthService(
            _userRepositoryMock.Object,
            _userProfileRepositoryMock.Object,
            _leaderboardRepositoryMock.Object,
            _jwtServiceMock.Object,
            _configurationMock.Object);
    }

    // Helper method to create password hash (same logic as AuthService)
    private static string HashPassword(string password)
    {
        byte[] salt = new byte[128 / 8];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8));

        return $"{Convert.ToBase64String(salt)}.{hashed}";
    }

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_ShouldCreateUser_WhenEmailDoesNotExist()
    {
        // Arrange
        var request = new RegisterRequest("test@example.com", "Password123!", "Test User");

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _jwtServiceMock
            .Setup(j => j.GenerateAccessToken(It.IsAny<ApplicationUser>()))
            .Returns("access_token");

        _jwtServiceMock
            .Setup(j => j.GenerateRefreshToken())
            .Returns("refresh_token");

        // Act
        var result = await _service.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");
        result.Email.Should().Be(request.Email);
        result.Role.Should().Be("Customer");
        result.FullName.Should().Be(request.FullName);

        _userRepositoryMock.Verify(r => r.AddAsync(It.Is<ApplicationUser>(u =>
            u.Email == request.Email &&
            u.Role == "Customer" &&
            u.IsActive == true)), Times.Once);

        _userProfileRepositoryMock.Verify(r => r.AddAsync(It.Is<UserProfile>(p =>
            p.FullName == request.FullName)), Times.Once);

        _leaderboardRepositoryMock.Verify(r => r.AddAsync(It.Is<Leaderboard>(l =>
            l.TotalPoints == 0)), Times.Once);

        _userRepositoryMock.Verify(r => r.SaveRefreshTokenAsync(It.IsAny<int>(), "refresh_token", It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldThrowException_WhenEmailAlreadyExists()
    {
        // Arrange
        var request = new RegisterRequest("existing@example.com", "Password123!", "Test User");

        var existingUser = new ApplicationUser { Email = request.Email };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(request.Email))
            .ReturnsAsync(existingUser);

        // Act
        Func<Task> act = async () => await _service.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email already exists");

        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_ShouldReturnAuthResponse_WhenCredentialsAreValid()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "Password123!");
        var correctPasswordHash = HashPassword("Password123!"); // Create actual hash

        var user = new ApplicationUser
        {
            UserId = 1,
            Email = request.Email,
            PasswordHash = correctPasswordHash, // Use actual hash
            Role = "Customer",
            IsActive = true
        };

        var userProfile = new UserProfile
        {
            UserId = 1,
            FullName = "Test User"
        };

        var leaderboard = new Leaderboard
        {
            UserId = 1,
            TotalPoints = 100
        };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        _jwtServiceMock
            .Setup(j => j.GenerateAccessToken(user))
            .Returns("access_token");

        _jwtServiceMock
            .Setup(j => j.GenerateRefreshToken())
            .Returns("refresh_token");

        _userProfileRepositoryMock
            .Setup(r => r.GetByUserIdAsync(user.UserId))
            .ReturnsAsync(userProfile);

        _leaderboardRepositoryMock
            .Setup(r => r.GetByUserIdAsync(user.UserId))
            .ReturnsAsync(leaderboard);

        // Act
        var result = await _service.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");
        result.Email.Should().Be(user.Email);
        result.Role.Should().Be(user.Role);
        result.FullName.Should().Be(userProfile.FullName);
        result.ContributionPoints.Should().Be(leaderboard.TotalPoints);

        _userRepositoryMock.Verify(r => r.UpdateAsync(It.Is<ApplicationUser>(u =>
            u.LastLoginAt.HasValue)), Times.Once);

        _userRepositoryMock.Verify(r => r.SaveRefreshTokenAsync(user.UserId, "refresh_token", It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowException_WhenUserDoesNotExist()
    {
        // Arrange
        var request = new LoginRequest("nonexistent@example.com", "Password123!");

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        Func<Task> act = async () => await _service.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowException_WhenUserIsInactive()
    {
        // Arrange
        var request = new LoginRequest("inactive@example.com", "Password123!");

        var inactiveUser = new ApplicationUser
        {
            Email = request.Email,
            IsActive = false
        };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(request.Email))
            .ReturnsAsync(inactiveUser);

        // Act
        Func<Task> act = async () => await _service.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials");
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowException_WhenPasswordIsIncorrect()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "WrongPassword123!");

        var user = new ApplicationUser
        {
            Email = request.Email,
            PasswordHash = "correct_hash",
            IsActive = true
        };

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(request.Email))
            .ReturnsAsync(user);

        // Act
        Func<Task> act = async () => await _service.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid credentials");
    }

    #endregion

    #region RefreshTokenAsync Tests

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnNewTokens_WhenRefreshTokenIsValid()
    {
        // Arrange
        var request = new RefreshTokenRequest("valid_refresh_token");

        var user = new ApplicationUser
        {
            UserId = 1,
            Email = "test@example.com",
            Role = "Customer",
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(1)
        };

        var userProfile = new UserProfile { UserId = 1, FullName = "Test User" };
        var leaderboard = new Leaderboard { UserId = 1, TotalPoints = 50 };

        _userRepositoryMock
            .Setup(r => r.GetByRefreshTokenAsync(request.RefreshToken))
            .ReturnsAsync(user);

        _jwtServiceMock
            .Setup(j => j.GenerateAccessToken(user))
            .Returns("new_access_token");

        _jwtServiceMock
            .Setup(j => j.GenerateRefreshToken())
            .Returns("new_refresh_token");

        _userProfileRepositoryMock
            .Setup(r => r.GetByUserIdAsync(user.UserId))
            .ReturnsAsync(userProfile);

        _leaderboardRepositoryMock
            .Setup(r => r.GetByUserIdAsync(user.UserId))
            .ReturnsAsync(leaderboard);

        // Act
        var result = await _service.RefreshTokenAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("new_access_token");
        result.RefreshToken.Should().Be("new_refresh_token");

        _userRepositoryMock.Verify(r => r.SaveRefreshTokenAsync(user.UserId, "new_refresh_token", It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldThrowException_WhenRefreshTokenIsInvalid()
    {
        // Arrange
        var request = new RefreshTokenRequest("invalid_refresh_token");

        _userRepositoryMock
            .Setup(r => r.GetByRefreshTokenAsync(request.RefreshToken))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        Func<Task> act = async () => await _service.RefreshTokenAsync(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid refresh token");
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldThrowException_WhenRefreshTokenIsExpired()
    {
        // Arrange
        var request = new RefreshTokenRequest("expired_refresh_token");

        var user = new ApplicationUser
        {
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(-1) // Expired
        };

        _userRepositoryMock
            .Setup(r => r.GetByRefreshTokenAsync(request.RefreshToken))
            .ReturnsAsync(user);

        // Act
        Func<Task> act = async () => await _service.RefreshTokenAsync(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Refresh token expired");
    }

    #endregion

    #region HasAdminAccountAsync Tests

    [Fact]
    public async Task HasAdminAccountAsync_ShouldReturnTrue_WhenAdminExists()
    {
        // Arrange
        var adminUser = new ApplicationUser
        {
            Role = "Admin",
            IsActive = true
        };

        var regularUser = new ApplicationUser
        {
            Role = "Customer",
            IsActive = true
        };

        var users = new List<ApplicationUser> { adminUser, regularUser };

        _userRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(users);

        // Act
        var result = await _service.HasAdminAccountAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasAdminAccountAsync_ShouldReturnFalse_WhenNoAdminExists()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new ApplicationUser { Role = "Customer", IsActive = true },
            new ApplicationUser { Role = "Customer", IsActive = true }
        };

        _userRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(users);

        // Act
        var result = await _service.HasAdminAccountAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasAdminAccountAsync_ShouldReturnFalse_WhenAdminIsInactive()
    {
        // Arrange
        var inactiveAdmin = new ApplicationUser
        {
            Role = "Admin",
            IsActive = false
        };

        var users = new List<ApplicationUser> { inactiveAdmin };

        _userRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(users);

        // Act
        var result = await _service.HasAdminAccountAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region InitializeAdminAsync Tests

    [Fact]
    public async Task InitializeAdminAsync_ShouldCreateAdmin_WhenNoAdminExistsAndKeyIsValid()
    {
        // Arrange
        var request = new InitializeAdminRequest
        {
            Email = "admin@example.com",
            Password = "AdminPass123!",
            FullName = "System Admin",
            InitializationKey = "valid_key"
        };

        _userRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<ApplicationUser>()); // No admin exists

        _configurationMock
            .Setup(c => c["AdminInitialization:SecretKey"])
            .Returns("valid_key");

        _userRepositoryMock
            .Setup(r => r.GetByEmailAsync(request.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _jwtServiceMock
            .Setup(j => j.GenerateAccessToken(It.IsAny<ApplicationUser>()))
            .Returns("admin_access_token");

        _jwtServiceMock
            .Setup(j => j.GenerateRefreshToken())
            .Returns("admin_refresh_token");

        // Act
        var result = await _service.InitializeAdminAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("admin_access_token");
        result.RefreshToken.Should().Be("admin_refresh_token");
        result.Email.Should().Be(request.Email);
        result.Role.Should().Be("Admin");
        result.FullName.Should().Be(request.FullName);

        _userRepositoryMock.Verify(r => r.AddAsync(It.Is<ApplicationUser>(u =>
            u.Email == request.Email &&
            u.Role == "Admin" &&
            u.IsActive == true)), Times.Once);

        _userProfileRepositoryMock.Verify(r => r.AddAsync(It.Is<UserProfile>(p =>
            p.FullName == request.FullName)), Times.Once);

        _leaderboardRepositoryMock.Verify(r => r.AddAsync(It.Is<Leaderboard>(l =>
            l.RankTitle == "System Administrator")), Times.Once);
    }

    [Fact]
    public async Task InitializeAdminAsync_ShouldThrowException_WhenAdminAlreadyExists()
    {
        // Arrange
        var request = new InitializeAdminRequest
        {
            Email = "admin@example.com",
            Password = "AdminPass123!",
            FullName = "System Admin",
            InitializationKey = "valid_key"
        };

        var existingAdmin = new ApplicationUser { Role = "Admin", IsActive = true };
        var users = new List<ApplicationUser> { existingAdmin };

        _userRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(users);

        // Act
        Func<Task> act = async () => await _service.InitializeAdminAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Admin account already exists. This endpoint is disabled.");
    }

    [Fact]
    public async Task InitializeAdminAsync_ShouldThrowException_WhenInitializationKeyIsInvalid()
    {
        // Arrange
        var request = new InitializeAdminRequest
        {
            Email = "admin@example.com",
            Password = "AdminPass123!",
            FullName = "System Admin",
            InitializationKey = "invalid_key"
        };

        _userRepositoryMock
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<ApplicationUser>()); // No admin exists

        _configurationMock
            .Setup(c => c["AdminInitialization:SecretKey"])
            .Returns("valid_key");

        // Act
        Func<Task> act = async () => await _service.InitializeAdminAsync(request);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid initialization key");
    }

    [Fact]
    public void GoogleTokenResponse_Should_Set_And_Get_Properties_Correctly()
    {
        // Arrange
        var response = new GoogleTokenResponse();

        // Act
        response.access_token = "test_access_token";
        response.id_token = "test_id_token";
        response.refresh_token = "test_refresh_token";
        response.expires_in = 3600;

        // Assert
        response.access_token.Should().Be("test_access_token");
        response.id_token.Should().Be("test_id_token");
        response.refresh_token.Should().Be("test_refresh_token");
        response.expires_in.Should().Be(3600);
    }

    #endregion
}

