using FluentAssertions;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using HealthSync.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HealthSync.Infrastructure.Tests.Repositories;

public class UserRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new UserRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnUser_WhenEmailExists()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Email = "test@example.com",
            PasswordHash = "hashedpassword",
            Role = "Customer",
            IsActive = true
        };
        await _context.ApplicationUsers.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEmailAsync("test@example.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnNull_WhenEmailDoesNotExist()
    {
        // Act
        var result = await _repository.GetByEmailAsync("nonexistent@example.com");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenIdExists()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Email = "test@example.com",
            PasswordHash = "hashedpassword",
            Role = "Customer",
            IsActive = true
        };
        await _context.ApplicationUsers.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(user.UserId);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(user.UserId);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenIdDoesNotExist()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ShouldAddUserToDatabase()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Email = "newuser@example.com",
            PasswordHash = "hashedpassword",
            Role = "Customer",
            IsActive = true
        };

        // Act
        await _repository.AddAsync(user);

        // Assert
        var addedUser = await _context.ApplicationUsers.FirstOrDefaultAsync(u => u.Email == "newuser@example.com");
        addedUser.Should().NotBeNull();
        addedUser!.Email.Should().Be("newuser@example.com");
    }

    [Fact]
    public async Task EmailExistsAsync_ShouldReturnTrue_WhenEmailExists()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Email = "existing@example.com",
            PasswordHash = "hashedpassword",
            Role = "Customer",
            IsActive = true
        };
        await _context.ApplicationUsers.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.EmailExistsAsync("existing@example.com");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_ShouldReturnFalse_WhenEmailDoesNotExist()
    {
        // Act
        var result = await _repository.EmailExistsAsync("nonexistent@example.com");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetByRefreshTokenAsync_ShouldReturnUser_WhenTokenIsValid()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Email = "test@example.com",
            PasswordHash = "hashedpassword",
            Role = "Customer",
            IsActive = true,
            RefreshToken = "validtoken",
            RefreshTokenExpiry = DateTime.UtcNow.AddHours(1)
        };
        await _context.ApplicationUsers.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByRefreshTokenAsync("validtoken");

        // Assert
        result.Should().NotBeNull();
        result!.RefreshToken.Should().Be("validtoken");
    }

    [Fact]
    public async Task GetByRefreshTokenAsync_ShouldReturnNull_WhenTokenIsExpired()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Email = "test@example.com",
            PasswordHash = "hashedpassword",
            Role = "Customer",
            IsActive = true,
            RefreshToken = "expiredtoken",
            RefreshTokenExpiry = DateTime.UtcNow.AddHours(-1)
        };
        await _context.ApplicationUsers.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByRefreshTokenAsync("expiredtoken");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveRefreshTokenAsync_ShouldUpdateTokenAndExpiry()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Email = "test@example.com",
            PasswordHash = "hashedpassword",
            Role = "Customer",
            IsActive = true
        };
        await _context.ApplicationUsers.AddAsync(user);
        await _context.SaveChangesAsync();

        var newToken = "newtoken";
        var expiry = DateTime.UtcNow.AddDays(7);

        // Act
        await _repository.SaveRefreshTokenAsync(user.UserId, newToken, expiry);

        // Assert
        var updatedUser = await _context.ApplicationUsers.FindAsync(user.UserId);
        updatedUser.Should().NotBeNull();
        updatedUser!.RefreshToken.Should().Be(newToken);
        updatedUser.RefreshTokenExpiry.Should().Be(expiry);
    }

    [Fact]
    public async Task SetActiveStatusAsync_ShouldUpdateUserStatus()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Email = "test@example.com",
            PasswordHash = "hashedpassword",
            Role = "Customer",
            IsActive = true
        };
        await _context.ApplicationUsers.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        await _repository.SetActiveStatusAsync(user.UserId, false);

        // Assert
        var updatedUser = await _context.ApplicationUsers.FindAsync(user.UserId);
        updatedUser.Should().NotBeNull();
        updatedUser!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetTotalWorkoutsAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Email = "test@example.com",
            PasswordHash = "hashedpassword",
            Role = "Customer",
            IsActive = true
        };
        await _context.ApplicationUsers.AddAsync(user);
        await _context.SaveChangesAsync();

        var workout1 = new WorkoutLog { UserId = user.UserId, WorkoutDate = DateTime.Today };
        var workout2 = new WorkoutLog { UserId = user.UserId, WorkoutDate = DateTime.Today.AddDays(-1) };
        await _context.WorkoutLogs.AddRangeAsync(workout1, workout2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetTotalWorkoutsAsync(user.UserId);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task GetActiveUsersAsync_ShouldReturnOnlyActiveUsers()
    {
        // Arrange
        var activeUser = new ApplicationUser
        {
            Email = "active@example.com",
            PasswordHash = "hashedpassword",
            Role = "Customer",
            IsActive = true
        };
        var inactiveUser = new ApplicationUser
        {
            Email = "inactive@example.com",
            PasswordHash = "hashedpassword",
            Role = "Customer",
            IsActive = false
        };
        await _context.ApplicationUsers.AddRangeAsync(activeUser, inactiveUser);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveUsersAsync();

        // Assert
        result.Should().HaveCount(1);
        result.First().Email.Should().Be("active@example.com");
    }
}

