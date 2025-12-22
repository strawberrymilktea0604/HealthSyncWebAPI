using HealthSync.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace HealthSync.Domain.Tests.Entities;

public class GoalTests
{
    [Fact]
    public void Goal_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var goal = new Goal();

        // Assert
        goal.GoalId.Should().Be(0);
        goal.UserId.Should().Be(0);
        goal.GoalType.Should().Be(GoalType.WeightLoss); // Default enum value
        goal.TargetValue.Should().Be(0);
        goal.Unit.Should().BeNull();
        goal.Status.Should().Be(GoalStatus.InProgress);
        goal.CreatedAt.Should().Be(default(DateTime));
        goal.UpdatedAt.Should().BeNull();
        goal.CompletedAt.Should().BeNull();
        goal.ProgressRecords.Should().NotBeNull();
        goal.ProgressRecords.Should().BeEmpty();
    }

    [Fact]
    public void Goal_ShouldAllowPropertyAssignment()
    {
        // Arrange
        var goal = new Goal();
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddDays(30);

        // Act
        goal.GoalId = 1;
        goal.UserId = 123;
        goal.GoalType = GoalType.WeightLoss;
        goal.TargetValue = 65.0m;
        goal.Unit = "kg";
        goal.StartDate = startDate;
        goal.EndDate = endDate;
        goal.Status = GoalStatus.InProgress;

        // Assert
        goal.GoalId.Should().Be(1);
        goal.UserId.Should().Be(123);
        goal.GoalType.Should().Be(GoalType.WeightLoss);
        goal.TargetValue.Should().Be(65.0m);
        goal.Unit.Should().Be("kg");
        goal.StartDate.Should().Be(startDate);
        goal.EndDate.Should().Be(endDate);
        goal.Status.Should().Be(GoalStatus.InProgress);
    }
}

public class EnumsTests
{
    [Theory]
    [InlineData(MuscleGroup.Chest)]
    [InlineData(MuscleGroup.Back)]
    [InlineData(MuscleGroup.Legs)]
    [InlineData(MuscleGroup.Shoulders)]
    [InlineData(MuscleGroup.Arms)]
    [InlineData(MuscleGroup.Core)]
    [InlineData(MuscleGroup.Cardio)]
    [InlineData(MuscleGroup.FullBody)]
    public void MuscleGroup_ShouldHaveValidValues(MuscleGroup muscleGroup)
    {
        // Arrange & Act & Assert
        Enum.IsDefined(typeof(MuscleGroup), muscleGroup).Should().BeTrue();
    }

    [Theory]
    [InlineData(DifficultyLevel.Beginner)]
    [InlineData(DifficultyLevel.Intermediate)]
    [InlineData(DifficultyLevel.Advanced)]
    public void DifficultyLevel_ShouldHaveValidValues(DifficultyLevel difficultyLevel)
    {
        // Arrange & Act & Assert
        Enum.IsDefined(typeof(DifficultyLevel), difficultyLevel).Should().BeTrue();
    }

    [Theory]
    [InlineData(GoalType.WeightLoss)]
    [InlineData(GoalType.WeightGain)]
    [InlineData(GoalType.MaintainWeight)]
    [InlineData(GoalType.BodyMeasurement)]
    public void GoalType_ShouldHaveValidValues(GoalType goalType)
    {
        // Arrange & Act & Assert
        Enum.IsDefined(typeof(GoalType), goalType).Should().BeTrue();
    }

    [Theory]
    [InlineData(GoalStatus.InProgress)]
    [InlineData(GoalStatus.Completed)]
    [InlineData(GoalStatus.Cancelled)]
    public void GoalStatus_ShouldHaveValidValues(GoalStatus goalStatus)
    {
        // Arrange & Act & Assert
        Enum.IsDefined(typeof(GoalStatus), goalStatus).Should().BeTrue();
    }
}

