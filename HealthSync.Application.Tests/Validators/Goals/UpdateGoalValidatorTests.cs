using FluentValidation.TestHelper;
using HealthSync.Application.DTOs.Goals;
using HealthSync.Application.Validators.Goals;
using HealthSync.Domain.Entities;
using Xunit;

namespace HealthSync.Application.Tests.Validators.Goals;

public class UpdateGoalValidatorTests
{
    private readonly UpdateGoalValidator _validator;

    public UpdateGoalValidatorTests()
    {
        _validator = new UpdateGoalValidator();
    }

    [Fact]
    public void Should_Pass_When_All_Valid_Data()
    {
        // Arrange
        var request = new UpdateGoalRequest
        {
            GoalType = GoalType.WeightLoss,
            TargetValue = 65.0m,
            Unit = "kg",
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 3, 1)
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_GoalType_Is_Invalid()
    {
        // Arrange
        var request = new UpdateGoalRequest
        {
            GoalType = (GoalType)999, // Invalid enum value
            TargetValue = 65.0m,
            Unit = "kg",
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 3, 1)
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.GoalType)
            .WithErrorMessage("Invalid goal type");
    }

    [Fact]
    public void Should_Fail_When_TargetValue_Is_Zero()
    {
        // Arrange
        var request = new UpdateGoalRequest
        {
            GoalType = GoalType.WeightLoss,
            TargetValue = 0,
            Unit = "kg",
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 3, 1)
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TargetValue)
            .WithErrorMessage("Target value must be greater than 0");
    }

    [Fact]
    public void Should_Fail_When_TargetValue_Is_Negative()
    {
        // Arrange
        var request = new UpdateGoalRequest
        {
            GoalType = GoalType.WeightLoss,
            TargetValue = -10,
            Unit = "kg",
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 3, 1)
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TargetValue)
            .WithErrorMessage("Target value must be greater than 0");
    }

    [Fact]
    public void Should_Fail_When_Unit_Is_Empty()
    {
        // Arrange
        var request = new UpdateGoalRequest
        {
            GoalType = GoalType.WeightLoss,
            TargetValue = 65.0m,
            Unit = "",
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 3, 1)
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Unit)
            .WithErrorMessage("Unit must be one of: kg, cm, %");
    }

    [Fact]
    public void Should_Fail_When_Unit_Is_Invalid()
    {
        // Arrange
        var request = new UpdateGoalRequest
        {
            GoalType = GoalType.WeightLoss,
            TargetValue = 65.0m,
            Unit = "invalid",
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 3, 1)
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Unit)
            .WithErrorMessage("Unit must be one of: kg, cm, %");
    }

    [Theory]
    [InlineData("kg")]
    [InlineData("cm")]
    [InlineData("%")]
    public void Should_Pass_When_Unit_Is_Valid(string validUnit)
    {
        // Arrange
        var request = new UpdateGoalRequest
        {
            GoalType = GoalType.WeightLoss,
            TargetValue = 65.0m,
            Unit = validUnit,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 3, 1)
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Unit);
    }

    [Fact]
    public void Should_Fail_When_StartDate_Is_Default()
    {
        // Arrange
        var request = new UpdateGoalRequest
        {
            GoalType = GoalType.WeightLoss,
            TargetValue = 65.0m,
            Unit = "kg",
            StartDate = default(DateTime),
            EndDate = new DateTime(2025, 3, 1)
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.StartDate)
            .WithErrorMessage("Start date is required");
    }

    [Fact]
    public void Should_Fail_When_EndDate_Is_Default()
    {
        // Arrange
        var request = new UpdateGoalRequest
        {
            GoalType = GoalType.WeightLoss,
            TargetValue = 65.0m,
            Unit = "kg",
            StartDate = new DateTime(2025, 1, 1),
            EndDate = default(DateTime)
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EndDate)
            .WithErrorMessage("End date must be after start date");
    }

    [Fact]
    public void Should_Fail_When_EndDate_Is_Before_StartDate()
    {
        // Arrange
        var request = new UpdateGoalRequest
        {
            GoalType = GoalType.WeightLoss,
            TargetValue = 65.0m,
            Unit = "kg",
            StartDate = new DateTime(2025, 3, 1),
            EndDate = new DateTime(2025, 1, 1)
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EndDate)
            .WithErrorMessage("End date must be after start date");
    }

    [Fact]
    public void Should_Fail_When_EndDate_Equals_StartDate()
    {
        // Arrange
        var request = new UpdateGoalRequest
        {
            GoalType = GoalType.WeightLoss,
            TargetValue = 65.0m,
            Unit = "kg",
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 1, 1)
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EndDate)
            .WithErrorMessage("End date must be after start date");
    }
}

