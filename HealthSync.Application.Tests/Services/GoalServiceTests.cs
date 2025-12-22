using HealthSync.Application.DTOs.Goals;
using HealthSync.Application.Interfaces;
using HealthSync.Application.Services;
using HealthSync.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace HealthSync.Application.Tests.Services;

public class GoalServiceTests
{
    private readonly Mock<IGoalRepository> _goalRepositoryMock;
    private readonly Mock<IUserProfileRepository> _userProfileRepositoryMock;
    private readonly Mock<ILogger<GoalService>> _loggerMock;
    private readonly GoalService _service;

    public GoalServiceTests()
    {
        _goalRepositoryMock = new Mock<IGoalRepository>();
        _userProfileRepositoryMock = new Mock<IUserProfileRepository>();
        _loggerMock = new Mock<ILogger<GoalService>>();

        _service = new GoalService(
            _goalRepositoryMock.Object,
            _userProfileRepositoryMock.Object);
    }

    [Fact]
    public async Task CreateGoalAsync_ShouldCreateWeightLossGoal_WhenValidRequest()
    {
        // Arrange
        var userId = 1;
        var request = new CreateGoalRequest
        {
            GoalType = GoalType.WeightLoss,
            TargetValue = 65,
            Unit = "kg",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(30)
        };

        var userProfile = new UserProfile
        {
            UserId = userId,
            CurrentWeightKg = 70
        };

        _userProfileRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(userProfile);

        _goalRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Goal>()))
            .Returns(Task.CompletedTask);

        _goalRepositoryMock
            .Setup(r => r.AddProgressRecordAsync(It.IsAny<ProgressRecord>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateGoalAsync(request, userId);

        // Assert
        result.Should().NotBeNull();
        result.GoalType.Should().Be(GoalType.WeightLoss);
        result.TargetValue.Should().Be(65);
        result.Status.Should().Be(GoalStatus.InProgress);

        _goalRepositoryMock.Verify(r => r.AddAsync(It.Is<Goal>(g =>
            g.UserId == userId &&
            g.GoalType == GoalType.WeightLoss &&
            g.TargetValue == 65)), Times.Once);

        _goalRepositoryMock.Verify(r => r.AddProgressRecordAsync(It.Is<ProgressRecord>(r =>
            r.GoalId == result.GoalId &&
            r.RecordedValue == 70)), Times.Once); // Initial weight
    }

    [Fact]
    public async Task CreateGoalAsync_ShouldThrowValidationException_WhenEndDateBeforeStartDate()
    {
        // Arrange
        var userId = 1;
        var request = new CreateGoalRequest
        {
            GoalType = GoalType.WeightLoss,
            TargetValue = 65,
            Unit = "kg",
            StartDate = DateTime.UtcNow.Date.AddDays(10),
            EndDate = DateTime.UtcNow.Date.AddDays(5) // End before start
        };

        // Act & Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.CreateGoalAsync(request, userId));
    }

    [Fact]
    public async Task CreateGoalAsync_ShouldThrowValidationException_WhenWeightLossTargetHigherThanCurrent()
    {
        // Arrange
        var userId = 1;
        var request = new CreateGoalRequest
        {
            GoalType = GoalType.WeightLoss,
            TargetValue = 75, // Higher than current weight
            Unit = "kg",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(30)
        };

        var userProfile = new UserProfile
        {
            UserId = userId,
            CurrentWeightKg = 70
        };

        _userProfileRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(userProfile);

        // Act & Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.CreateGoalAsync(request, userId));
    }

    [Fact]
    public async Task RecordProgressAsync_ShouldRecordProgressAndCompleteGoal_WhenWeightLossTargetReached()
    {
        // Arrange
        var userId = 1;
        var goalId = 1;
        var request = new RecordProgressRequest
        {
            GoalId = goalId,
            RecordDate = DateTime.UtcNow.Date,
            RecordedValue = 65 // Target reached
        };

        var goal = new Goal
        {
            GoalId = goalId,
            UserId = userId,
            GoalType = GoalType.WeightLoss,
            TargetValue = 65,
            StartDate = DateTime.UtcNow.Date.AddDays(-10),
            EndDate = DateTime.UtcNow.Date.AddDays(20),
            Status = GoalStatus.InProgress
        };

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync(goal);

        _goalRepositoryMock
            .Setup(r => r.GetProgressRecordsByGoalIdAsync(goalId))
            .ReturnsAsync(new List<ProgressRecord>()); // No existing records

        _goalRepositoryMock
            .Setup(r => r.AddProgressRecordAsync(It.IsAny<ProgressRecord>()))
            .Returns(Task.CompletedTask);

        _goalRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Goal>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.RecordProgressAsync(request, userId);

        // Assert
        result.Should().NotBeNull();
        result.RecordedValue.Should().Be(65);

        _goalRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Goal>(g =>
            g.Status == GoalStatus.Completed)), Times.Once);
    }

    [Fact]
    public async Task RecordProgressAsync_ShouldThrowValidationException_WhenDuplicateDate()
    {
        // Arrange
        var userId = 1;
        var goalId = 1;
        var recordDate = DateTime.UtcNow.Date;
        var request = new RecordProgressRequest
        {
            GoalId = goalId,
            RecordDate = recordDate,
            RecordedValue = 68
        };

        var goal = new Goal
        {
            GoalId = goalId,
            UserId = userId,
            GoalType = GoalType.WeightLoss,
            TargetValue = 65,
            StartDate = DateTime.UtcNow.Date.AddDays(-10),
            EndDate = DateTime.UtcNow.Date.AddDays(20),
            Status = GoalStatus.InProgress
        };

        var existingRecord = new ProgressRecord
        {
            ProgressRecordId = 1,
            GoalId = goalId,
            RecordDate = recordDate,
            RecordedValue = 70
        };

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync(goal);

        _goalRepositoryMock
            .Setup(r => r.GetProgressRecordsByGoalIdAsync(goalId))
            .ReturnsAsync(new List<ProgressRecord> { existingRecord });

        // Act & Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.RecordProgressAsync(request, userId));
    }

    [Fact]
    public async Task GetProgressChartAsync_ShouldCalculateProgressPercentCorrectly_ForWeightLoss()
    {
        // Arrange
        var userId = 1;
        var goalId = 1;

        var goal = new Goal
        {
            GoalId = goalId,
            UserId = userId,
            GoalType = GoalType.WeightLoss,
            TargetValue = 65,
            Unit = "kg",
            StartDate = DateTime.UtcNow.Date.AddDays(-20),
            EndDate = DateTime.UtcNow.Date.AddDays(10),
            Status = GoalStatus.InProgress
        };

        var records = new List<ProgressRecord>
        {
            new ProgressRecord
            {
                ProgressRecordId = 1,
                GoalId = goalId,
                RecordDate = DateTime.UtcNow.Date.AddDays(-20),
                RecordedValue = 70 // Initial weight
            },
            new ProgressRecord
            {
                ProgressRecordId = 2,
                GoalId = goalId,
                RecordDate = DateTime.UtcNow.Date,
                RecordedValue = 67.5m // Current weight
            }
        };

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync(goal);

        _goalRepositoryMock
            .Setup(r => r.GetProgressRecordsByGoalIdAsync(goalId))
            .ReturnsAsync(records);

        // Act
        var result = await _service.GetProgressChartAsync(goalId, userId);

        // Assert
        result.Should().NotBeNull();
        result.GoalId.Should().Be(goalId);
        result.ProgressPercent.Should().Be(50); // (70 - 67.5) / (70 - 65) * 100 = 50%
        result.ProgressRecords.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetProgressChartAsync_ShouldCalculateProgressPercentCorrectly_ForWeightGain()
    {
        // Arrange
        var userId = 1;
        var goalId = 1;

        var goal = new Goal
        {
            GoalId = goalId,
            UserId = userId,
            GoalType = GoalType.WeightGain,
            TargetValue = 75,
            Unit = "kg",
            StartDate = DateTime.UtcNow.Date.AddDays(-20),
            EndDate = DateTime.UtcNow.Date.AddDays(10),
            Status = GoalStatus.InProgress
        };

        var records = new List<ProgressRecord>
        {
            new ProgressRecord
            {
                ProgressRecordId = 1,
                GoalId = goalId,
                RecordDate = DateTime.UtcNow.Date.AddDays(-20),
                RecordedValue = 70 // Initial weight
            },
            new ProgressRecord
            {
                ProgressRecordId = 2,
                GoalId = goalId,
                RecordDate = DateTime.UtcNow.Date,
                RecordedValue = 72.5m // Current weight
            }
        };

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync(goal);

        _goalRepositoryMock
            .Setup(r => r.GetProgressRecordsByGoalIdAsync(goalId))
            .ReturnsAsync(records);

        // Act
        var result = await _service.GetProgressChartAsync(goalId, userId);

        // Assert
        result.Should().NotBeNull();
        result.ProgressPercent.Should().Be(50); // (72.5 - 70) / (75 - 70) * 100 = 50%
    }

    [Fact]
    public async Task GetProgressChartAsync_ShouldCalculateProgressPercentCorrectly_ForBodyMeasurement()
    {
        // Arrange
        var userId = 1;
        var goalId = 1;

        var goal = new Goal
        {
            GoalId = goalId,
            UserId = userId,
            GoalType = GoalType.BodyMeasurement,
            TargetValue = 80, // Target waist cm
            Unit = "cm",
            StartDate = DateTime.UtcNow.Date.AddDays(-20),
            EndDate = DateTime.UtcNow.Date.AddDays(10),
            Status = GoalStatus.InProgress
        };

        var records = new List<ProgressRecord>
        {
            new ProgressRecord
            {
                ProgressRecordId = 1,
                GoalId = goalId,
                RecordDate = DateTime.UtcNow.Date.AddDays(-20),
                RecordedValue = 0 // Initial (not set)
            },
            new ProgressRecord
            {
                ProgressRecordId = 2,
                GoalId = goalId,
                RecordDate = DateTime.UtcNow.Date,
                RecordedValue = 40 // Current waist cm
            }
        };

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync(goal);

        _goalRepositoryMock
            .Setup(r => r.GetProgressRecordsByGoalIdAsync(goalId))
            .ReturnsAsync(records);

        // Act
        var result = await _service.GetProgressChartAsync(goalId, userId);

        // Assert
        result.Should().NotBeNull();
        result.ProgressPercent.Should().Be(50); // 40 / 80 * 100 = 50%
    }

    [Fact]
    public async Task GetUserGoalsAsync_ShouldReturnUserGoals_WhenGoalsExist()
    {
        // Arrange
        var userId = 1;
        var goals = new List<Goal>
        {
            new Goal
            {
                GoalId = 1,
                UserId = userId,
                GoalType = GoalType.WeightLoss,
                TargetValue = 65,
                Unit = "kg",
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(30),
                Status = GoalStatus.InProgress,
                CreatedAt = DateTime.UtcNow,
                ProgressRecords = new List<ProgressRecord>()
            }
        };

        _goalRepositoryMock
            .Setup(r => r.GetUserGoalsAsync(userId))
            .ReturnsAsync(goals);

        // Act
        var result = await _service.GetUserGoalsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().GoalId.Should().Be(1);
        result.First().GoalType.Should().Be(GoalType.WeightLoss);
        result.First().TargetValue.Should().Be(65);
        result.First().Unit.Should().Be("kg");
        result.First().Status.Should().Be(GoalStatus.InProgress);
    }

    [Fact]
    public async Task GetUserGoalsAsync_ShouldReturnEmptyList_WhenNoGoals()
    {
        // Arrange
        var userId = 1;

        _goalRepositoryMock
            .Setup(r => r.GetUserGoalsAsync(userId))
            .ReturnsAsync(new List<Goal>());

        // Act
        var result = await _service.GetUserGoalsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}

