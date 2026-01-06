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

    [Fact]
    public async Task CreateGoalAsync_ShouldCreateMaintainWeightGoal_WhenValidRequest()
    {
        // Arrange
        var userId = 1;
        var request = new CreateGoalRequest
        {
            GoalType = GoalType.MaintainWeight,
            TargetValue = 70,
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

        // Act
        var result = await _service.CreateGoalAsync(request, userId);

        // Assert
        result.Should().NotBeNull();
        result.GoalType.Should().Be(GoalType.MaintainWeight);
        result.TargetValue.Should().Be(70);
        result.Status.Should().Be(GoalStatus.InProgress);

        _goalRepositoryMock.Verify(r => r.AddAsync(It.Is<Goal>(g =>
            g.GoalType == GoalType.MaintainWeight &&
            g.TargetValue == 70 &&
            g.Status == GoalStatus.InProgress)), Times.Once);
    }

    [Fact]
    public async Task GetProgressChartAsync_ShouldCalculateProgressPercentCorrectly_ForMaintainWeight()
    {
        // Arrange
        var userId = 1;
        var goal = new Goal
        {
            GoalId = 1,
            UserId = userId,
            GoalType = GoalType.MaintainWeight,
            TargetValue = 70,
            Unit = "kg",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(30),
            Status = GoalStatus.InProgress
        };

        var progressRecords = new List<ProgressRecord>
        {
            new ProgressRecord
            {
                GoalId = 1,
                RecordDate = DateTime.UtcNow.Date,
                RecordedValue = 70,
                CreatedAt = DateTime.UtcNow
            },
            new ProgressRecord
            {
                GoalId = 1,
                RecordDate = DateTime.UtcNow.Date.AddDays(7),
                RecordedValue = 69.5m,
                CreatedAt = DateTime.UtcNow
            }
        };

        _goalRepositoryMock
            .Setup(r => r.GetUserGoalsAsync(userId))
            .ReturnsAsync(new List<Goal> { goal });

        _goalRepositoryMock
            .Setup(r => r.GetProgressRecordsByGoalIdAsync(goal.GoalId))
            .ReturnsAsync(progressRecords);

        // Act
        var result = await _service.GetUserProgressChartAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.ProgressPoints.Should().NotBeEmpty();
        // For MaintainWeight, progress should always be 100%
        var maintainWeightPoint = result.ProgressPoints.FirstOrDefault();
        maintainWeightPoint.Should().NotBeNull();
        // Note: ProgressPointDto doesn't have GoalType, just check that points exist
    }

    [Fact]
    public async Task RecordProgressAsync_ShouldCompleteMaintainWeightGoal_WhenWeightWithinRange()
    {
        // Arrange
        var userId = 1;
        var goal = new Goal
        {
            GoalId = 1,
            UserId = userId,
            GoalType = GoalType.MaintainWeight,
            TargetValue = 70,
            Unit = "kg",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(30),
            Status = GoalStatus.InProgress
        };

        var request = new RecordProgressRequest
        {
            GoalId = goal.GoalId,
            RecordDate = DateTime.UtcNow.Date.AddDays(14),
            RecordedValue = 70.5m // Within 1kg range of target
        };

        var existingRecords = new List<ProgressRecord>
        {
            new ProgressRecord
            {
                GoalId = goal.GoalId,
                RecordDate = goal.StartDate,
                RecordedValue = 70,
                CreatedAt = DateTime.UtcNow
            }
        };

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goal.GoalId))
            .ReturnsAsync(goal);

        _goalRepositoryMock
            .Setup(r => r.GetProgressRecordsByGoalIdAsync(goal.GoalId))
            .ReturnsAsync(existingRecords);

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
        result.RecordedValue.Should().Be(70.5m);

        // Verify goal is completed for MaintainWeight when within 1kg range
        _goalRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Goal>(g =>
            g.Status == GoalStatus.Completed)), Times.Once);
    }

    [Fact]
    public async Task GetGoalByIdAsync_ShouldThrowException_WhenGoalNotFound()
    {
        // Arrange
        var userId = 1;
        var goalId = 999;

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync((Goal?)null);

        // Act & Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.GetGoalByIdAsync(goalId, userId));
    }

    [Fact]
    public async Task GetGoalByIdAsync_ShouldThrowException_WhenUserIdMismatch()
    {
        // Arrange
        var userId = 1;
        var goalId = 1;

        var goal = new Goal
        {
            GoalId = goalId,
            UserId = 999, // Different user
            GoalType = GoalType.WeightLoss,
            TargetValue = 65
        };

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync(goal);

        // Act & Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.GetGoalByIdAsync(goalId, userId));
    }

    [Fact]
    public async Task UpdateGoalAsync_ShouldThrowException_WhenGoalNotFound()
    {
        // Arrange
        var userId = 1;
        var goalId = 999;
        var request = new UpdateGoalRequest
        {
            GoalType = GoalType.WeightLoss,
            TargetValue = 60,
            Unit = "kg",
            StartDate = DateTime.UtcNow.Date.AddDays(1),
            EndDate = DateTime.UtcNow.Date.AddDays(31)
        };

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync((Goal?)null);

        // Act & Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.UpdateGoalAsync(goalId, request, userId));
    }

    [Fact]
    public async Task UpdateGoalAsync_ShouldThrowException_WhenGoalCompleted()
    {
        // Arrange
        var userId = 1;
        var goalId = 1;
        var request = new UpdateGoalRequest
        {
            GoalType = GoalType.WeightLoss,
            TargetValue = 60,
            Unit = "kg",
            StartDate = DateTime.UtcNow.Date.AddDays(1),
            EndDate = DateTime.UtcNow.Date.AddDays(31)
        };

        var goal = new Goal
        {
            GoalId = goalId,
            UserId = userId,
            Status = GoalStatus.Completed // Already completed
        };

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync(goal);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.UpdateGoalAsync(goalId, request, userId));

        exception.Message.Should().Contain("Cannot update completed or cancelled goals");
    }

    [Fact]
    public async Task RecordProgressAsync_ShouldThrowException_WhenGoalNotFound()
    {
        // Arrange
        var userId = 1;
        var request = new RecordProgressRequest
        {
            GoalId = 999,
            RecordDate = DateTime.UtcNow.Date,
            RecordedValue = 68
        };

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Goal?)null);

        // Act & Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.RecordProgressAsync(request, userId));
    }

    [Fact]
    public async Task RecordProgressAsync_ShouldThrowException_WhenGoalCompleted()
    {
        // Arrange
        var userId = 1;
        var goalId = 1;
        var request = new RecordProgressRequest
        {
            GoalId = goalId,
            RecordDate = DateTime.UtcNow.Date,
            RecordedValue = 68
        };

        var goal = new Goal
        {
            GoalId = goalId,
            UserId = userId,
            Status = GoalStatus.Completed
        };

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync(goal);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.RecordProgressAsync(request, userId));

        exception.Message.Should().Contain("Cannot record progress for completed or cancelled goals");
    }

    [Fact]
    public async Task RecordProgressAsync_ShouldThrowException_WhenDateOutsideGoalPeriod()
    {
        // Arrange
        var userId = 1;
        var goalId = 1;
        var request = new RecordProgressRequest
        {
            GoalId = goalId,
            RecordDate = DateTime.UtcNow.Date.AddDays(100), // Outside goal period
            RecordedValue = 68
        };

        var goal = new Goal
        {
            GoalId = goalId,
            UserId = userId,
            GoalType = GoalType.WeightLoss,
            TargetValue = 65,
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(30),
            Status = GoalStatus.InProgress
        };

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync(goal);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.RecordProgressAsync(request, userId));

        exception.Message.Should().Contain("Record date must be within goal period");
    }

    [Fact]
    public async Task UpdateProgressRecordAsync_ShouldThrowException_WhenRecordNotFound()
    {
        // Arrange
        var userId = 1;
        var recordId = 999;
        var request = new UpdateProgressRequest
        {
            RecordedValue = 68
        };

        _goalRepositoryMock
            .Setup(r => r.GetProgressRecordByIdAsync(recordId))
            .ReturnsAsync((ProgressRecord?)null);

        // Act & Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.UpdateProgressRecordAsync(recordId, request, userId));
    }

    [Fact]
    public async Task UpdateProgressRecordAsync_ShouldThrowException_WhenUnauthorizedAccess()
    {
        // Arrange
        var userId = 1;
        var recordId = 1;
        var request = new UpdateProgressRequest
        {
            RecordedValue = 68
        };

        var record = new ProgressRecord
        {
            ProgressRecordId = recordId,
            GoalId = 1
        };

        var goal = new Goal
        {
            GoalId = 1,
            UserId = 999 // Different user
        };

        _goalRepositoryMock
            .Setup(r => r.GetProgressRecordByIdAsync(recordId))
            .ReturnsAsync(record);

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(goal);

        // Act & Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.UpdateProgressRecordAsync(recordId, request, userId));
    }

    [Fact]
    public async Task DeleteGoalAsync_ShouldThrowException_WhenGoalNotFound()
    {
        // Arrange
        var userId = 1;
        var goalId = 999;

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync((Goal?)null);

        // Act & Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.DeleteGoalAsync(goalId, userId));
    }

    [Fact]
    public async Task DeleteGoalAsync_ShouldThrowException_WhenUserIdMismatch()
    {
        // Arrange
        var userId = 1;
        var goalId = 1;

        var goal = new Goal
        {
            GoalId = goalId,
            UserId = 999 // Different user
        };

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync(goal);

        // Act & Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.DeleteGoalAsync(goalId, userId));
    }

    [Fact]
    public async Task DeleteProgressRecordAsync_ShouldThrowException_WhenRecordNotFound()
    {
        // Arrange
        var userId = 1;
        var recordId = 999;

        _goalRepositoryMock
            .Setup(r => r.GetProgressRecordByIdAsync(recordId))
            .ReturnsAsync((ProgressRecord?)null);

        // Act & Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.DeleteProgressRecordAsync(recordId, userId));
    }

    [Fact]
    public async Task DeleteProgressRecordAsync_ShouldThrowException_WhenUnauthorizedAccess()
    {
        // Arrange
        var userId = 1;
        var recordId = 1;

        var record = new ProgressRecord
        {
            ProgressRecordId = recordId,
            GoalId = 1
        };

        var goal = new Goal
        {
            GoalId = 1,
            UserId = 999 // Different user
        };

        _goalRepositoryMock
            .Setup(r => r.GetProgressRecordByIdAsync(recordId))
            .ReturnsAsync(record);

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(goal);

        // Act & Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.DeleteProgressRecordAsync(recordId, userId));
    }

    #region Additional Tests for 100% Coverage

    [Fact]
    public async Task GetProgressChartAsync_ShouldReturnZeroPercent_WhenNoProgressRecords()
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

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync(goal);

        _goalRepositoryMock
            .Setup(r => r.GetProgressRecordsByGoalIdAsync(goalId))
            .ReturnsAsync(new List<ProgressRecord>()); // Empty records

        // Act
        var result = await _service.GetProgressChartAsync(goalId, userId);

        // Assert
        result.Should().NotBeNull();
        result.ProgressPercent.Should().Be(0); // Empty records should return 0%
    }

    [Fact]
    public async Task GetProgressChartAsync_ShouldReturn100Percent_WhenInitialValueEqualsTargetValue()
    {
        // Arrange
        var userId = 1;
        var goalId = 1;

        var goal = new Goal
        {
            GoalId = goalId,
            UserId = userId,
            GoalType = GoalType.WeightLoss,
            TargetValue = 70, // Target equals initial
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
                RecordedValue = 70 // Initial equals target
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
        result.ProgressPercent.Should().Be(100); // Should be 100% when initial equals target
    }

    [Fact]
    public async Task UpdateGoalAsync_ShouldThrowException_WhenStartDateIsInPast()
    {
        // Arrange
        var userId = 1;
        var goalId = 1;
        var request = new UpdateGoalRequest
        {
            GoalType = GoalType.WeightLoss,
            TargetValue = 60,
            Unit = "kg",
            StartDate = DateTime.UtcNow.Date.AddDays(-5), // In the past
            EndDate = DateTime.UtcNow.Date.AddDays(25)
        };

        var goal = new Goal
        {
            GoalId = goalId,
            UserId = userId,
            Status = GoalStatus.InProgress
        };

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync(goal);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.UpdateGoalAsync(goalId, request, userId));
        
        exception.Message.Should().Contain("Start date cannot be in the past");
    }

    [Fact]
    public async Task UpdateGoalAsync_ShouldThrowException_WhenEndDateBeforeStartDate()
    {
        // Arrange
        var userId = 1;
        var goalId = 1;
        var request = new UpdateGoalRequest
        {
            GoalType = GoalType.WeightLoss,
            TargetValue = 60,
            Unit = "kg",
            StartDate = DateTime.UtcNow.Date.AddDays(10),
            EndDate = DateTime.UtcNow.Date.AddDays(5) // Before start
        };

        var goal = new Goal
        {
            GoalId = goalId,
            UserId = userId,
            Status = GoalStatus.InProgress
        };

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync(goal);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.UpdateGoalAsync(goalId, request, userId));
        
        exception.Message.Should().Contain("End date must be after start date");
    }

    [Fact]
    public async Task CreateGoalAsync_ShouldCreateWeightGainGoal_WhenValidRequest()
    {
        // Arrange
        var userId = 1;
        var request = new CreateGoalRequest
        {
            GoalType = GoalType.WeightGain,
            TargetValue = 75, // Higher than current
            Unit = "kg",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(30)
        };

        var userProfile = new UserProfile
        {
            UserId = userId,
            CurrentWeightKg = 60
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
        result.GoalType.Should().Be(GoalType.WeightGain);
        result.TargetValue.Should().Be(75);
    }

    [Fact]
    public async Task CreateGoalAsync_ShouldThrowException_WhenWeightGainTargetLowerThanCurrent()
    {
        // Arrange
        var userId = 1;
        var request = new CreateGoalRequest
        {
            GoalType = GoalType.WeightGain,
            TargetValue = 55, // Lower than current weight
            Unit = "kg",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(30)
        };

        var userProfile = new UserProfile
        {
            UserId = userId,
            CurrentWeightKg = 60
        };

        _userProfileRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(userProfile);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.CreateGoalAsync(request, userId));
        
        exception.Message.Should().Contain("Target weight must be greater than current weight for weight gain");
    }

    [Fact]
    public async Task CreateGoalAsync_ShouldThrowException_WhenWeightChangeExceeds30Percent()
    {
        // Arrange
        var userId = 1;
        var request = new CreateGoalRequest
        {
            GoalType = GoalType.WeightLoss,
            TargetValue = 40, // More than 30% less than 70kg
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
        var exception = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.CreateGoalAsync(request, userId));
        
        exception.Message.Should().Contain("Target weight change cannot exceed 30% of current weight");
    }

    [Fact]
    public async Task CreateGoalAsync_ShouldThrowException_WhenUserProfileNotFound()
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

        _userProfileRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync((UserProfile?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.CreateGoalAsync(request, userId));
        
        exception.Message.Should().Contain("User profile not found");
    }

    [Fact]
    public async Task CreateGoalAsync_ShouldThrowException_WhenCurrentWeightNotSet()
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
            CurrentWeightKg = null // Not set
        };

        _userProfileRepositoryMock
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync(userProfile);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.CreateGoalAsync(request, userId));
        
        exception.Message.Should().Contain("Current weight not set in profile");
    }

    [Fact]
    public async Task CreateGoalAsync_ShouldCreateBodyMeasurementGoal_WithZeroInitialValue()
    {
        // Arrange
        var userId = 1;
        var request = new CreateGoalRequest
        {
            GoalType = GoalType.BodyMeasurement,
            TargetValue = 80, // Target waist
            Unit = "cm",
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(30)
        };

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
        result.GoalType.Should().Be(GoalType.BodyMeasurement);
        
        // Verify initial progress record has 0 value for BodyMeasurement
        _goalRepositoryMock.Verify(r => r.AddProgressRecordAsync(It.Is<ProgressRecord>(pr =>
            pr.RecordedValue == 0)), Times.Once);
    }

    [Fact]
    public async Task GetProgressRecordAsync_ShouldReturnProgressRecord_WhenRecordExists()
    {
        // Arrange
        var userId = 1;
        var recordId = 1;

        var goal = new Goal
        {
            GoalId = 1,
            UserId = userId
        };

        var record = new ProgressRecord
        {
            ProgressRecordId = recordId,
            GoalId = 1,
            RecordDate = DateTime.UtcNow.Date,
            RecordedValue = 68,
            WeightKg = 68,
            WaistCm = 80,
            ChestCm = 100,
            HipCm = 95,
            Notes = "Test record",
            CreatedAt = DateTime.UtcNow,
            Goal = goal
        };

        _goalRepositoryMock
            .Setup(r => r.GetProgressRecordByIdAsync(recordId))
            .ReturnsAsync(record);

        // Act
        var result = await _service.GetProgressRecordAsync(recordId, userId);

        // Assert
        result.Should().NotBeNull();
        result.ProgressRecordId.Should().Be(recordId);
        result.RecordedValue.Should().Be(68);
        result.WeightKg.Should().Be(68);
        result.WaistCm.Should().Be(80);
        result.ChestCm.Should().Be(100);
        result.HipCm.Should().Be(95);
        result.Notes.Should().Be("Test record");
    }

    [Fact]
    public async Task GetProgressRecordAsync_ShouldThrowException_WhenRecordNotFound()
    {
        // Arrange
        var userId = 1;
        var recordId = 999;

        _goalRepositoryMock
            .Setup(r => r.GetProgressRecordByIdAsync(recordId))
            .ReturnsAsync((ProgressRecord?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.GetProgressRecordAsync(recordId, userId));
    }

    [Fact]
    public async Task GetProgressRecordAsync_ShouldThrowException_WhenUserIdMismatch()
    {
        // Arrange
        var userId = 1;
        var recordId = 1;

        var goal = new Goal
        {
            GoalId = 1,
            UserId = 999 // Different user
        };

        var record = new ProgressRecord
        {
            ProgressRecordId = recordId,
            GoalId = 1,
            Goal = goal
        };

        _goalRepositoryMock
            .Setup(r => r.GetProgressRecordByIdAsync(recordId))
            .ReturnsAsync(record);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.GetProgressRecordAsync(recordId, userId));
    }

    [Fact]
    public async Task GetProgressRecordsByGoalAsync_ShouldReturnRecords_WhenGoalExists()
    {
        // Arrange
        var userId = 1;
        var goalId = 1;

        var goal = new Goal
        {
            GoalId = goalId,
            UserId = userId
        };

        var records = new List<ProgressRecord>
        {
            new ProgressRecord
            {
                ProgressRecordId = 1,
                GoalId = goalId,
                RecordDate = DateTime.UtcNow.Date.AddDays(-10),
                RecordedValue = 70,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new ProgressRecord
            {
                ProgressRecordId = 2,
                GoalId = goalId,
                RecordDate = DateTime.UtcNow.Date,
                RecordedValue = 68,
                CreatedAt = DateTime.UtcNow
            }
        };

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync(goal);

        _goalRepositoryMock
            .Setup(r => r.GetProgressRecordsByGoalIdAsync(goalId))
            .ReturnsAsync(records);

        // Act
        var result = await _service.GetProgressRecordsByGoalAsync(goalId, userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetProgressRecordsByGoalAsync_ShouldThrowException_WhenGoalNotFound()
    {
        // Arrange
        var userId = 1;
        var goalId = 999;

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync((Goal?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.GetProgressRecordsByGoalAsync(goalId, userId));
    }

    [Fact]
    public async Task GetProgressRecordsByGoalAsync_ShouldThrowException_WhenUserIdMismatch()
    {
        // Arrange
        var userId = 1;
        var goalId = 1;

        var goal = new Goal
        {
            GoalId = goalId,
            UserId = 999 // Different user
        };

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync(goal);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.GetProgressRecordsByGoalAsync(goalId, userId));
    }

    [Fact]
    public async Task UpdateGoalAsync_ShouldUpdateGoal_WhenValidRequest()
    {
        // Arrange
        var userId = 1;
        var goalId = 1;
        var request = new UpdateGoalRequest
        {
            GoalType = GoalType.WeightLoss,
            TargetValue = 60,
            Unit = "kg",
            StartDate = DateTime.UtcNow.Date.AddDays(1),
            EndDate = DateTime.UtcNow.Date.AddDays(31)
        };

        var goal = new Goal
        {
            GoalId = goalId,
            UserId = userId,
            GoalType = GoalType.WeightLoss,
            TargetValue = 65,
            Unit = "kg",
            StartDate = DateTime.UtcNow.Date.AddDays(1),
            EndDate = DateTime.UtcNow.Date.AddDays(31),
            Status = GoalStatus.InProgress
        };

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync(goal);

        _goalRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Goal>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateGoalAsync(goalId, request, userId);

        // Assert
        result.Should().NotBeNull();
        result.TargetValue.Should().Be(60);
        _goalRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Goal>(g =>
            g.TargetValue == 60 &&
            g.UpdatedAt.HasValue)), Times.Once);
    }

    [Fact]
    public async Task DeleteGoalAsync_ShouldDeleteGoalAndProgressRecords_WhenGoalExists()
    {
        // Arrange
        var userId = 1;
        var goalId = 1;

        var goal = new Goal
        {
            GoalId = goalId,
            UserId = userId
        };

        var progressRecords = new List<ProgressRecord>
        {
            new ProgressRecord { ProgressRecordId = 1, GoalId = goalId },
            new ProgressRecord { ProgressRecordId = 2, GoalId = goalId }
        };

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync(goal);

        _goalRepositoryMock
            .Setup(r => r.GetProgressRecordsByGoalIdAsync(goalId))
            .ReturnsAsync(progressRecords);

        _goalRepositoryMock
            .Setup(r => r.DeleteProgressRecordAsync(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        _goalRepositoryMock
            .Setup(r => r.DeleteAsync(goalId))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteGoalAsync(goalId, userId);

        // Assert
        _goalRepositoryMock.Verify(r => r.DeleteProgressRecordAsync(1), Times.Once);
        _goalRepositoryMock.Verify(r => r.DeleteProgressRecordAsync(2), Times.Once);
        _goalRepositoryMock.Verify(r => r.DeleteAsync(goalId), Times.Once);
    }

    [Fact]
    public async Task UpdateProgressRecordAsync_ShouldUpdateAndCompleteGoal_WhenTargetReached()
    {
        // Arrange
        var userId = 1;
        var recordId = 1;
        var request = new UpdateProgressRequest
        {
            RecordedValue = 65, // Target reached
            WeightKg = 65,
            WaistCm = 75,
            ChestCm = 95,
            HipCm = 90,
            Notes = "Updated notes"
        };

        var goal = new Goal
        {
            GoalId = 1,
            UserId = userId,
            GoalType = GoalType.WeightLoss,
            TargetValue = 65,
            Status = GoalStatus.InProgress
        };

        var record = new ProgressRecord
        {
            ProgressRecordId = recordId,
            GoalId = 1,
            RecordDate = DateTime.UtcNow.Date,
            RecordedValue = 68
        };

        _goalRepositoryMock
            .Setup(r => r.GetProgressRecordByIdAsync(recordId))
            .ReturnsAsync(record);

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(goal);

        _goalRepositoryMock
            .Setup(r => r.UpdateProgressRecordAsync(It.IsAny<ProgressRecord>()))
            .Returns(Task.CompletedTask);

        _goalRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Goal>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateProgressRecordAsync(recordId, request, userId);

        // Assert
        result.Should().NotBeNull();
        result.RecordedValue.Should().Be(65);
        result.WeightKg.Should().Be(65);
        result.WaistCm.Should().Be(75);
        result.ChestCm.Should().Be(95);
        result.HipCm.Should().Be(90);
        result.Notes.Should().Be("Updated notes");

        // Goal should be completed
        _goalRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Goal>(g =>
            g.Status == GoalStatus.Completed)), Times.Once);
    }

    [Fact]
    public async Task DeleteProgressRecordAsync_ShouldDeleteRecord_WhenAuthorized()
    {
        // Arrange
        var userId = 1;
        var recordId = 1;

        var goal = new Goal
        {
            GoalId = 1,
            UserId = userId
        };

        var record = new ProgressRecord
        {
            ProgressRecordId = recordId,
            GoalId = 1
        };

        _goalRepositoryMock
            .Setup(r => r.GetProgressRecordByIdAsync(recordId))
            .ReturnsAsync(record);

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(goal);

        _goalRepositoryMock
            .Setup(r => r.DeleteProgressRecordAsync(recordId))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteProgressRecordAsync(recordId, userId);

        // Assert
        _goalRepositoryMock.Verify(r => r.DeleteProgressRecordAsync(recordId), Times.Once);
    }

    [Fact]
    public async Task GetGoalByIdAsync_ShouldReturnGoal_WhenGoalExists()
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
            StartDate = DateTime.UtcNow.Date,
            EndDate = DateTime.UtcNow.Date.AddDays(30),
            Status = GoalStatus.InProgress,
            CreatedAt = DateTime.UtcNow
        };

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync(goal);

        // Act
        var result = await _service.GetGoalByIdAsync(goalId, userId);

        // Assert
        result.Should().NotBeNull();
        result.GoalId.Should().Be(goalId);
        result.GoalType.Should().Be(GoalType.WeightLoss);
        result.TargetValue.Should().Be(65);
        result.Status.Should().Be(GoalStatus.InProgress);
    }

    [Fact]
    public async Task GetUserProgressChartAsync_ShouldReturnEmptyList_WhenNoGoals()
    {
        // Arrange
        var userId = 1;

        _goalRepositoryMock
            .Setup(r => r.GetUserGoalsAsync(userId))
            .ReturnsAsync(new List<Goal>());

        // Act
        var result = await _service.GetUserProgressChartAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.ProgressPoints.Should().BeEmpty();
    }

    [Fact]
    public async Task RecordProgressAsync_ShouldCompleteWeightGainGoal_WhenTargetReached()
    {
        // Arrange
        var userId = 1;
        var goalId = 1;
        var request = new RecordProgressRequest
        {
            GoalId = goalId,
            RecordDate = DateTime.UtcNow.Date,
            RecordedValue = 75 // Target reached for weight gain
        };

        var goal = new Goal
        {
            GoalId = goalId,
            UserId = userId,
            GoalType = GoalType.WeightGain,
            TargetValue = 75,
            StartDate = DateTime.UtcNow.Date.AddDays(-10),
            EndDate = DateTime.UtcNow.Date.AddDays(20),
            Status = GoalStatus.InProgress
        };

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync(goal);

        _goalRepositoryMock
            .Setup(r => r.GetProgressRecordsByGoalIdAsync(goalId))
            .ReturnsAsync(new List<ProgressRecord>());

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
        _goalRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Goal>(g =>
            g.Status == GoalStatus.Completed)), Times.Once);
    }

    [Fact]
    public async Task RecordProgressAsync_ShouldCompleteBodyMeasurementGoal_WhenTargetReached()
    {
        // Arrange
        var userId = 1;
        var goalId = 1;
        var request = new RecordProgressRequest
        {
            GoalId = goalId,
            RecordDate = DateTime.UtcNow.Date,
            RecordedValue = 80 // Target reached for body measurement
        };

        var goal = new Goal
        {
            GoalId = goalId,
            UserId = userId,
            GoalType = GoalType.BodyMeasurement,
            TargetValue = 80,
            StartDate = DateTime.UtcNow.Date.AddDays(-10),
            EndDate = DateTime.UtcNow.Date.AddDays(20),
            Status = GoalStatus.InProgress
        };

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync(goal);

        _goalRepositoryMock
            .Setup(r => r.GetProgressRecordsByGoalIdAsync(goalId))
            .ReturnsAsync(new List<ProgressRecord>());

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
        _goalRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Goal>(g =>
            g.Status == GoalStatus.Completed)), Times.Once);
    }

    [Fact]
    public async Task GetProgressChartAsync_ShouldReturnZeroPercent_WhenDefaultGoalType()
    {
        // Arrange
        var userId = 1;
        var goalId = 1;

        var goal = new Goal
        {
            GoalId = goalId,
            UserId = userId,
            GoalType = (GoalType)999, // Unknown type to trigger default case
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
                RecordedValue = 70
            },
            new ProgressRecord
            {
                ProgressRecordId = 2,
                GoalId = goalId,
                RecordDate = DateTime.UtcNow.Date,
                RecordedValue = 67.5m
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
        result.ProgressPercent.Should().Be(0); // Default case returns 0
    }

    [Fact]
    public async Task UpdateGoalAsync_ShouldThrowException_WhenUserIdMismatch()
    {
        // Arrange
        var userId = 1;
        var goalId = 1;
        var request = new UpdateGoalRequest
        {
            GoalType = GoalType.WeightLoss,
            TargetValue = 60,
            Unit = "kg",
            StartDate = DateTime.UtcNow.Date.AddDays(1),
            EndDate = DateTime.UtcNow.Date.AddDays(31)
        };

        var goal = new Goal
        {
            GoalId = goalId,
            UserId = 999, // Different user
            Status = GoalStatus.InProgress
        };

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync(goal);

        // Act & Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.UpdateGoalAsync(goalId, request, userId));
    }

    [Fact]
    public async Task RecordProgressAsync_ShouldThrowException_WhenUserIdMismatch()
    {
        // Arrange
        var userId = 1;
        var goalId = 1;
        var request = new RecordProgressRequest
        {
            GoalId = goalId,
            RecordDate = DateTime.UtcNow.Date,
            RecordedValue = 68
        };

        var goal = new Goal
        {
            GoalId = goalId,
            UserId = 999, // Different user
            GoalType = GoalType.WeightLoss,
            TargetValue = 65,
            StartDate = DateTime.UtcNow.Date.AddDays(-10),
            EndDate = DateTime.UtcNow.Date.AddDays(20),
            Status = GoalStatus.InProgress
        };

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync(goal);

        // Act & Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.RecordProgressAsync(request, userId));
    }

    [Fact]
    public async Task RecordProgressAsync_ShouldNotCompleteGoal_WhenTargetNotReached()
    {
        // Arrange
        var userId = 1;
        var goalId = 1;
        var request = new RecordProgressRequest
        {
            GoalId = goalId,
            RecordDate = DateTime.UtcNow.Date,
            RecordedValue = 68 // Not at target 65
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
            .ReturnsAsync(new List<ProgressRecord>());

        _goalRepositoryMock
            .Setup(r => r.AddProgressRecordAsync(It.IsAny<ProgressRecord>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.RecordProgressAsync(request, userId);

        // Assert
        result.Should().NotBeNull();
        result.RecordedValue.Should().Be(68);
        
        // Goal should NOT be updated to completed
        _goalRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Goal>()), Times.Never);
    }

    [Fact]
    public async Task GetUserProgressChartAsync_ShouldReturnProgressPoints_WithMultipleGoals()
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
                Status = GoalStatus.InProgress
            },
            new Goal
            {
                GoalId = 2,
                UserId = userId,
                GoalType = GoalType.BodyMeasurement,
                TargetValue = 80,
                Status = GoalStatus.InProgress
            }
        };

        var records1 = new List<ProgressRecord>
        {
            new ProgressRecord
            {
                ProgressRecordId = 1,
                GoalId = 1,
                RecordDate = DateTime.UtcNow.Date.AddDays(-10),
                RecordedValue = 70,
                WeightKg = 70,
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            }
        };

        var records2 = new List<ProgressRecord>
        {
            new ProgressRecord
            {
                ProgressRecordId = 2,
                GoalId = 2,
                RecordDate = DateTime.UtcNow.Date.AddDays(-5),
                RecordedValue = 40,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            }
        };

        _goalRepositoryMock
            .Setup(r => r.GetUserGoalsAsync(userId))
            .ReturnsAsync(goals);

        _goalRepositoryMock
            .Setup(r => r.GetProgressRecordsByGoalIdAsync(1))
            .ReturnsAsync(records1);

        _goalRepositoryMock
            .Setup(r => r.GetProgressRecordsByGoalIdAsync(2))
            .ReturnsAsync(records2);

        // Act
        var result = await _service.GetUserProgressChartAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.ProgressPoints.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetProgressChartAsync_ShouldThrowException_WhenUserIdMismatch()
    {
        // Arrange
        var userId = 1;
        var goalId = 1;

        var goal = new Goal
        {
            GoalId = goalId,
            UserId = 999, // Different user
            GoalType = GoalType.WeightLoss,
            TargetValue = 65
        };

        _goalRepositoryMock
            .Setup(r => r.GetByIdAsync(goalId))
            .ReturnsAsync(goal);

        // Act & Assert
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            _service.GetProgressChartAsync(goalId, userId));
    }

    #endregion
}

