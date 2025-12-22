using FluentValidation.TestHelper;
using HealthSync.Application.DTOs.Users;
using HealthSync.Application.Validators.Users;
using Xunit;

namespace HealthSync.Application.Tests.Validators.Users;

public class UpdateUserProfileValidatorTests
{
    private readonly UpdateUserProfileValidator _validator;

    public UpdateUserProfileValidatorTests()
    {
        _validator = new UpdateUserProfileValidator();
    }

    [Fact]
    public void Should_Pass_When_All_Valid_Data()
    {
        // Arrange
        var request = new UpdateUserProfileRequest(
            FullName: "John Doe",
            Gender: "Male",
            DateOfBirth: new DateTime(1990, 1, 1),
            HeightCm: 175,
            CurrentWeightKg: 70,
            ActivityLevel: null,
            AvatarUrl: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_FullName_Is_Empty()
    {
        // Arrange
        var request = new UpdateUserProfileRequest(
            FullName: "",
            Gender: "Male",
            DateOfBirth: new DateTime(1990, 1, 1),
            HeightCm: 175,
            CurrentWeightKg: 70,
            ActivityLevel: null,
            AvatarUrl: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FullName)
            .WithErrorMessage("Full name is required");
    }

    [Fact]
    public void Should_Fail_When_FullName_Exceeds_MaxLength()
    {
        // Arrange
        var request = new UpdateUserProfileRequest(
            FullName: new string('A', 101), // 101 characters
            Gender: "Male",
            DateOfBirth: new DateTime(1990, 1, 1),
            HeightCm: 175,
            CurrentWeightKg: 70,
            ActivityLevel: null,
            AvatarUrl: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FullName)
            .WithErrorMessage("Full name cannot exceed 100 characters");
    }

    [Fact]
    public void Should_Fail_When_Gender_Is_Invalid()
    {
        // Arrange
        var request = new UpdateUserProfileRequest(
            FullName: "John Doe",
            Gender: "Invalid",
            DateOfBirth: new DateTime(1990, 1, 1),
            HeightCm: 175,
            CurrentWeightKg: 70,
            ActivityLevel: null,
            AvatarUrl: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Gender)
            .WithErrorMessage("Gender must be either 'Male', 'Female', 'Other' or null");
    }

    [Fact]
    public void Should_Pass_When_Gender_Is_Null()
    {
        // Arrange
        var request = new UpdateUserProfileRequest(
            FullName: "John Doe",
            Gender: null,
            DateOfBirth: new DateTime(1990, 1, 1),
            HeightCm: 175,
            CurrentWeightKg: 70,
            ActivityLevel: null,
            AvatarUrl: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Gender);
    }

    [Fact]
    public void Should_Fail_When_DateOfBirth_Is_Under_13()
    {
        // Arrange
        var request = new UpdateUserProfileRequest(
            FullName: "John Doe",
            Gender: "Male",
            DateOfBirth: DateTime.UtcNow.AddYears(-12), // 12 years old
            HeightCm: 175,
            CurrentWeightKg: 70,
            ActivityLevel: null,
            AvatarUrl: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth)
            .WithErrorMessage("User must be at least 13 years old");
    }

    [Fact]
    public void Should_Pass_When_DateOfBirth_Is_Null()
    {
        // Arrange
        var request = new UpdateUserProfileRequest(
            FullName: "John Doe",
            Gender: "Male",
            DateOfBirth: null,
            HeightCm: 175,
            CurrentWeightKg: 70,
            ActivityLevel: null,
            AvatarUrl: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DateOfBirth);
    }

    [Fact]
    public void Should_Fail_When_Height_Is_Below_Minimum()
    {
        // Arrange
        var request = new UpdateUserProfileRequest(
            FullName: "John Doe",
            Gender: "Male",
            DateOfBirth: new DateTime(1990, 1, 1),
            HeightCm: 25, // Below 30cm
            CurrentWeightKg: 70,
            ActivityLevel: null,
            AvatarUrl: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.HeightCm)
            .WithErrorMessage("Height must be between 30cm and 300cm");
    }

    [Fact]
    public void Should_Fail_When_Height_Is_Above_Maximum()
    {
        // Arrange
        var request = new UpdateUserProfileRequest(
            FullName: "John Doe",
            Gender: "Male",
            DateOfBirth: new DateTime(1990, 1, 1),
            HeightCm: 350, // Above 300cm
            CurrentWeightKg: 70,
            ActivityLevel: null,
            AvatarUrl: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.HeightCm)
            .WithErrorMessage("Height must be between 30cm and 300cm");
    }

    [Fact]
    public void Should_Pass_When_Height_Is_Null()
    {
        // Arrange
        var request = new UpdateUserProfileRequest(
            FullName: "John Doe",
            Gender: "Male",
            DateOfBirth: new DateTime(1990, 1, 1),
            HeightCm: null,
            CurrentWeightKg: 70,
            ActivityLevel: null,
            AvatarUrl: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.HeightCm);
    }

    [Fact]
    public void Should_Fail_When_Weight_Is_Below_Minimum()
    {
        // Arrange
        var request = new UpdateUserProfileRequest(
            FullName: "John Doe",
            Gender: "Male",
            DateOfBirth: new DateTime(1990, 1, 1),
            HeightCm: 175,
            CurrentWeightKg: 15, // Below 20kg
            ActivityLevel: null,
            AvatarUrl: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CurrentWeightKg)
            .WithErrorMessage("Weight must be between 20kg and 500kg");
    }

    [Fact]
    public void Should_Fail_When_Weight_Is_Above_Maximum()
    {
        // Arrange
        var request = new UpdateUserProfileRequest(
            FullName: "John Doe",
            Gender: "Male",
            DateOfBirth: new DateTime(1990, 1, 1),
            HeightCm: 175,
            CurrentWeightKg: 600, // Above 500kg
            ActivityLevel: null,
            AvatarUrl: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CurrentWeightKg)
            .WithErrorMessage("Weight must be between 20kg and 500kg");
    }

    [Fact]
    public void Should_Pass_When_Weight_Is_Null()
    {
        // Arrange
        var request = new UpdateUserProfileRequest(
            FullName: "John Doe",
            Gender: "Male",
            DateOfBirth: new DateTime(1990, 1, 1),
            HeightCm: 175,
            CurrentWeightKg: null,
            ActivityLevel: null,
            AvatarUrl: null
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CurrentWeightKg);
    }
}

