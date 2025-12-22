using FluentValidation.TestHelper;
using HealthSync.Application.DTOs.Exercises;
using HealthSync.Application.Validators.Exercises;
using Xunit;

namespace HealthSync.Application.Tests.Validators.Exercises;

public class UpdateExerciseRequestValidatorTests
{
    private readonly UpdateExerciseRequestValidator _validator;

    public UpdateExerciseRequestValidatorTests()
    {
        _validator = new UpdateExerciseRequestValidator();
    }

    [Fact]
    public void Should_Not_Have_Error_When_All_Fields_Are_Null()
    {
        // Arrange
        var request = new UpdateExerciseRequest();

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_Name_Exceeds_MaxLength()
    {
        // Arrange
        var longName = new string('a', 101);
        var request = new UpdateExerciseRequest
        {
            Name = longName
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Exercise name must not exceed 100 characters");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Name_Is_Valid()
    {
        // Arrange
        var request = new UpdateExerciseRequest
        {
            Name = "Bench Press"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Should_Have_Error_When_MuscleGroup_Is_Invalid()
    {
        // Arrange
        var request = new UpdateExerciseRequest
        {
            MuscleGroup = "InvalidMuscle"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MuscleGroup)
            .WithErrorMessage("Invalid muscle group");
    }

    [Fact]
    public void Should_Not_Have_Error_When_MuscleGroup_Is_Valid()
    {
        // Arrange
        var request = new UpdateExerciseRequest
        {
            MuscleGroup = "Chest"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MuscleGroup);
    }

    [Fact]
    public void Should_Have_Error_When_Difficulty_Is_Invalid()
    {
        // Arrange
        var request = new UpdateExerciseRequest
        {
            Difficulty = "InvalidDifficulty"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Difficulty)
            .WithErrorMessage("Invalid difficulty level");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Difficulty_Is_Valid()
    {
        // Arrange
        var request = new UpdateExerciseRequest
        {
            Difficulty = "Beginner"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Difficulty);
    }

    [Fact]
    public void Should_Have_Error_When_Equipment_Is_Invalid()
    {
        // Arrange
        var request = new UpdateExerciseRequest
        {
            Equipment = "InvalidEquipment"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Equipment)
            .WithErrorMessage("Invalid equipment");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Equipment_Is_Valid()
    {
        // Arrange
        var request = new UpdateExerciseRequest
        {
            Equipment = "Barbell"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Equipment);
    }

    [Fact]
    public void Should_Have_Error_When_Description_Exceeds_MaxLength()
    {
        // Arrange
        var longDescription = new string('a', 501);
        var request = new UpdateExerciseRequest
        {
            Description = longDescription
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description must not exceed 500 characters");
    }

    [Fact]
    public void Should_Have_Error_When_Instructions_Exceed_MaxLength()
    {
        // Arrange
        var longInstructions = new string('a', 2001);
        var request = new UpdateExerciseRequest
        {
            Instructions = longInstructions
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Instructions)
            .WithErrorMessage("Instructions must not exceed 2000 characters");
    }

    [Fact]
    public void Should_Have_Error_When_ImageUrl_Is_Invalid()
    {
        // Arrange
        var request = new UpdateExerciseRequest
        {
            ImageUrl = "invalid-url"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ImageUrl)
            .WithErrorMessage("Invalid image URL");
    }

    [Fact]
    public void Should_Not_Have_Error_When_ImageUrl_Is_Valid()
    {
        // Arrange
        var request = new UpdateExerciseRequest
        {
            ImageUrl = "https://example.com/image.jpg"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ImageUrl);
    }

    [Fact]
    public void Should_Have_Error_When_VideoUrl_Is_Invalid()
    {
        // Arrange
        var request = new UpdateExerciseRequest
        {
            VideoUrl = "invalid-url"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.VideoUrl)
            .WithErrorMessage("Invalid video URL");
    }

    [Fact]
    public void Should_Have_Error_When_CaloriesPerMinute_Is_Zero()
    {
        // Arrange
        var request = new UpdateExerciseRequest
        {
            CaloriesPerMinute = 0
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CaloriesPerMinute)
            .WithErrorMessage("Calories per minute must be greater than 0");
    }

    [Fact]
    public void Should_Have_Error_When_CaloriesPerMinute_Is_Negative()
    {
        // Arrange
        var request = new UpdateExerciseRequest
        {
            CaloriesPerMinute = -1
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CaloriesPerMinute)
            .WithErrorMessage("Calories per minute must be greater than 0");
    }

    [Fact]
    public void Should_Not_Have_Error_When_CaloriesPerMinute_Is_Positive()
    {
        // Arrange
        var request = new UpdateExerciseRequest
        {
            CaloriesPerMinute = 5.5m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CaloriesPerMinute);
    }
}

