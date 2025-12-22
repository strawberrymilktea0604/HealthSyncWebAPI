using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using HealthSync.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Xunit;

namespace HealthSync.Infrastructure.Tests.Repositories;

public class GoalRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly GoalRepository _repository;

    public GoalRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new GoalRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnGoal_WhenIdExists()
    {
        // Arrange
        var goal = new Goal
        {
            UserId = 1,
            GoalType = GoalType.WeightLoss,
            TargetValue = 65.0m,
            Unit = "kg",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(3),
            Status = GoalStatus.InProgress
        };
        await _context.Goals.AddAsync(goal);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(goal.GoalId);

        // Assert
        result.Should().NotBeNull();
        result!.GoalId.Should().Be(goal.GoalId);
        result.GoalType.Should().Be(GoalType.WeightLoss);
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
    public async Task GetUserGoalsAsync_ShouldReturnGoalsForUser()
    {
        // Arrange
        var user1 = new ApplicationUser { Email = "user1@example.com", PasswordHash = "hash", Role = "Customer", IsActive = true };
        var user2 = new ApplicationUser { Email = "user2@example.com", PasswordHash = "hash", Role = "Customer", IsActive = true };
        await _context.ApplicationUsers.AddRangeAsync(user1, user2);
        await _context.SaveChangesAsync();

        var goal1 = new Goal
        {
            UserId = user1.UserId,
            GoalType = GoalType.WeightLoss,
            TargetValue = 65.0m,
            Unit = "kg",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(3),
            Status = GoalStatus.InProgress
        };
        var goal2 = new Goal
        {
            UserId = user1.UserId,
            GoalType = GoalType.WeightGain,
            TargetValue = 70.0m,
            Unit = "kg",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(3),
            Status = GoalStatus.InProgress
        };
        var goal3 = new Goal
        {
            UserId = user2.UserId,
            GoalType = GoalType.MaintainWeight,
            TargetValue = 68.0m,
            Unit = "kg",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(3),
            Status = GoalStatus.InProgress
        };
        await _context.Goals.AddRangeAsync(goal1, goal2, goal3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetUserGoalsAsync(user1.UserId);

        // Assert
        result.Should().HaveCount(2);
        result.All(g => g.UserId == user1.UserId).Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_ShouldAddGoalToDatabase()
    {
        // Arrange
        var goal = new Goal
        {
            UserId = 1,
            GoalType = GoalType.WeightLoss,
            TargetValue = 65.0m,
            Unit = "kg",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(3),
            Status = GoalStatus.InProgress
        };

        // Act
        await _repository.AddAsync(goal);

        // Assert
        var addedGoal = await _context.Goals.FirstOrDefaultAsync(g => g.GoalType == GoalType.WeightLoss);
        addedGoal.Should().NotBeNull();
        addedGoal!.TargetValue.Should().Be(65.0m);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateGoalInDatabase()
    {
        // Arrange
        var goal = new Goal
        {
            UserId = 1,
            GoalType = GoalType.WeightLoss,
            TargetValue = 65.0m,
            Unit = "kg",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(3),
            Status = GoalStatus.InProgress
        };
        await _context.Goals.AddAsync(goal);
        await _context.SaveChangesAsync();

        // Act
        goal.Status = GoalStatus.Completed;
        await _repository.UpdateAsync(goal);

        // Assert
        var updatedGoal = await _context.Goals.FindAsync(goal.GoalId);
        updatedGoal.Should().NotBeNull();
        updatedGoal!.Status.Should().Be(GoalStatus.Completed);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveGoalFromDatabase()
    {
        // Arrange
        var goal = new Goal
        {
            UserId = 1,
            GoalType = GoalType.WeightLoss,
            TargetValue = 65.0m,
            Unit = "kg",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(3),
            Status = GoalStatus.InProgress
        };
        await _context.Goals.AddAsync(goal);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(goal.GoalId);

        // Assert
        var deletedGoal = await _context.Goals.FindAsync(goal.GoalId);
        deletedGoal.Should().BeNull();
    }

    [Fact]
    public async Task GetProgressRecordByIdAsync_ShouldReturnProgressRecord_WhenIdExists()
    {
        // Arrange
        var goal = new Goal
        {
            UserId = 1,
            GoalType = GoalType.WeightLoss,
            TargetValue = 65.0m,
            Unit = "kg",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(3),
            Status = GoalStatus.InProgress
        };
        await _context.Goals.AddAsync(goal);
        await _context.SaveChangesAsync();

        var progressRecord = new ProgressRecord
        {
            GoalId = goal.GoalId,
            RecordDate = DateTime.Today,
            RecordedValue = 70.0m,
            Notes = "Initial weight"
        };
        await _context.ProgressRecords.AddAsync(progressRecord);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetProgressRecordByIdAsync(progressRecord.ProgressRecordId);

        // Assert
        result.Should().NotBeNull();
        result!.RecordedValue.Should().Be(70.0m);
    }

    [Fact]
    public async Task GetProgressRecordsByGoalIdAsync_ShouldReturnRecordsForGoal()
    {
        // Arrange
        var goal = new Goal
        {
            UserId = 1,
            GoalType = GoalType.WeightLoss,
            TargetValue = 65.0m,
            Unit = "kg",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(3),
            Status = GoalStatus.InProgress
        };
        await _context.Goals.AddAsync(goal);
        await _context.SaveChangesAsync();

        var record1 = new ProgressRecord
        {
            GoalId = goal.GoalId,
            RecordDate = DateTime.Today,
            RecordedValue = 70.0m
        };
        var record2 = new ProgressRecord
        {
            GoalId = goal.GoalId,
            RecordDate = DateTime.Today.AddDays(7),
            RecordedValue = 68.5m
        };
        await _context.ProgressRecords.AddRangeAsync(record1, record2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetProgressRecordsByGoalIdAsync(goal.GoalId);

        // Assert
        result.Should().HaveCount(2);
        result.First().RecordedValue.Should().Be(70.0m);
        result.Last().RecordedValue.Should().Be(68.5m);
    }

    [Fact]
    public async Task AddProgressRecordAsync_ShouldAddRecordToDatabase()
    {
        // Arrange
        var goal = new Goal
        {
            UserId = 1,
            GoalType = GoalType.WeightLoss,
            TargetValue = 65.0m,
            Unit = "kg",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(3),
            Status = GoalStatus.InProgress
        };
        await _context.Goals.AddAsync(goal);
        await _context.SaveChangesAsync();

        var progressRecord = new ProgressRecord
        {
            GoalId = goal.GoalId,
            RecordDate = DateTime.Today,
            RecordedValue = 70.0m,
            Notes = "Initial weight"
        };

        // Act
        await _repository.AddProgressRecordAsync(progressRecord);

        // Assert
        var addedRecord = await _context.ProgressRecords.FirstOrDefaultAsync(pr => pr.GoalId == goal.GoalId);
        addedRecord.Should().NotBeNull();
        addedRecord!.RecordedValue.Should().Be(70.0m);
    }

    [Fact]
    public async Task UpdateProgressRecordAsync_ShouldUpdateRecordInDatabase()
    {
        // Arrange
        var goal = new Goal
        {
            UserId = 1,
            GoalType = GoalType.WeightLoss,
            TargetValue = 65.0m,
            Unit = "kg",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(3),
            Status = GoalStatus.InProgress
        };
        await _context.Goals.AddAsync(goal);
        await _context.SaveChangesAsync();

        var progressRecord = new ProgressRecord
        {
            GoalId = goal.GoalId,
            RecordDate = DateTime.Today,
            RecordedValue = 70.0m,
            Notes = "Initial weight"
        };
        await _context.ProgressRecords.AddAsync(progressRecord);
        await _context.SaveChangesAsync();

        // Act
        progressRecord.RecordedValue = 69.5m;
        await _repository.UpdateProgressRecordAsync(progressRecord);

        // Assert
        var updatedRecord = await _context.ProgressRecords.FindAsync(progressRecord.ProgressRecordId);
        updatedRecord.Should().NotBeNull();
        updatedRecord!.RecordedValue.Should().Be(69.5m);
    }

    [Fact]
    public async Task DeleteProgressRecordAsync_ShouldRemoveRecordFromDatabase()
    {
        // Arrange
        var goal = new Goal
        {
            UserId = 1,
            GoalType = GoalType.WeightLoss,
            TargetValue = 65.0m,
            Unit = "kg",
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(3),
            Status = GoalStatus.InProgress
        };
        await _context.Goals.AddAsync(goal);
        await _context.SaveChangesAsync();

        var progressRecord = new ProgressRecord
        {
            GoalId = goal.GoalId,
            RecordDate = DateTime.Today,
            RecordedValue = 70.0m
        };
        await _context.ProgressRecords.AddAsync(progressRecord);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteProgressRecordAsync(progressRecord.ProgressRecordId);

        // Assert
        var deletedRecord = await _context.ProgressRecords.FindAsync(progressRecord.ProgressRecordId);
        deletedRecord.Should().BeNull();
    }
}

