using FluentAssertions;
using HealthSync.Application.DTOs.Challenges;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using HealthSync.WebApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Text.Json;
using System.Security.Claims;

namespace HealthSync.WebApi.Tests.Controllers;

public class ChallengesControllerTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ChallengesController _controller;

    public ChallengesControllerTests()
    {
        // Setup InMemory Database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);

        // Seed test data
        SeedTestData();

        // Create controller
        _controller = new ChallengesController(_db);

        // Setup authenticated user
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Customer")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    private void SeedTestData()
    {
        // Seed Challenge
        var challenge = new Challenge
        {
            ChallengeId = 1,
            Title = "30-Day Running Challenge",
            Description = "Run 100km in 30 days",
            ChallengeType = ChallengeType.Workout,
            StartDate = new DateTime(2025, 12, 1),
            EndDate = new DateTime(2025, 12, 31),
            Status = ChallengeStatus.Open,
            Criteria = "Upload screenshot from running app showing total distance >= 100km",
            MaxParticipants = 100,
            RewardDescription = "Certificate",
            CreatedAt = DateTime.UtcNow
        };
        _db.Challenges.Add(challenge);

        // Seed User
        var user = new ApplicationUser
        {
            UserId = 1,
            Email = "test@example.com",
            PasswordHash = "hashedpassword",
            Role = "Customer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.ApplicationUsers.Add(user);

        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetOpenChallenges_ShouldReturnOk_WhenChallengesRetrieved()
    {
        // Arrange
        var pageNumber = 1;
        var pageSize = 20;

        // Act
        var result = await _controller.GetOpenChallenges(pageNumber, pageSize);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = root.GetProperty("data");
        var items = data.GetProperty("items");
        items.EnumerateArray().Should().HaveCount(1);
        var firstChallenge = items.EnumerateArray().First();
        firstChallenge.GetProperty("Title").GetString().Should().Be("30-Day Running Challenge");
        firstChallenge.GetProperty("Status").GetInt32().Should().Be((int)ChallengeStatus.Open);
    }

    [Fact]
    public async Task GetOpenChallenges_ShouldReturnBadRequest_WhenPageNumberInvalid()
    {
        // Act
        var result = await _controller.GetOpenChallenges(pageNumber: 0);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var json = JsonSerializer.Serialize(badRequestResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("Page number must be >= 1");
    }

    [Fact]
    public async Task GetChallenge_ShouldReturnOk_WhenChallengeExists()
    {
        // Arrange
        var challengeId = 1;

        // Act
        var result = await _controller.GetChallenge(challengeId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeTrue();
        var data = root.GetProperty("data");
        data.GetProperty("ChallengeId").GetInt32().Should().Be(challengeId);
        data.GetProperty("Title").GetString().Should().Be("30-Day Running Challenge");
        data.GetProperty("ChallengeType").GetInt32().Should().Be((int)ChallengeType.Workout);
    }

    [Fact]
    public async Task GetChallenge_ShouldReturnNotFound_WhenChallengeNotExists()
    {
        // Arrange
        var challengeId = 999;

        // Act
        var result = await _controller.GetChallenge(challengeId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var json = JsonSerializer.Serialize(notFoundResult.Value);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.GetProperty("message").GetString().Should().Be("Challenge not found");
    }
}