using FluentValidation.TestHelper;
using HealthSync.Application.DTOs.Goals;
using HealthSync.Application.Validators.Goals;
using Xunit;

namespace HealthSync.Application.Tests.Validators.Goals;

public class UpdateProgressValidatorTests
{
    private readonly UpdateProgressValidator _validator;

    public UpdateProgressValidatorTests()
    {
        _validator = new UpdateProgressValidator();
    }

    [Fact]
    public void Should_Pass_When_All_Valid_Data()
    {
        // Arrange
        var request = new UpdateProgressRequest
        {
            RecordedValue = 65.0m,
            WeightKg = 65.0m,
            WaistCm = 80.0m,
            ChestCm = 90.0m,
            HipCm = 95.0m,
            Notes = "Good progress this week"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Pass_When_Only_Required_Fields()
    {
        // Arrange
        var request = new UpdateProgressRequest
        {
            RecordedValue = 65.0m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_RecordedValue_Is_Zero()
    {
        // Arrange
        var request = new UpdateProgressRequest
        {
            RecordedValue = 0
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RecordedValue)
            .WithErrorMessage("Recorded value must be greater than 0");
    }

    [Fact]
    public void Should_Fail_When_RecordedValue_Is_Negative()
    {
        // Arrange
        var request = new UpdateProgressRequest
        {
            RecordedValue = -10
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RecordedValue)
            .WithErrorMessage("Recorded value must be greater than 0");
    }

    [Fact]
    public void Should_Pass_When_RecordedValue_Is_Null()
    {
        // Arrange
        var request = new UpdateProgressRequest
        {
            RecordedValue = null
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.RecordedValue);
    }

    [Fact]
    public void Should_Fail_When_WeightKg_Is_Below_Minimum()
    {
        // Arrange
        var request = new UpdateProgressRequest
        {
            RecordedValue = 65.0m,
            WeightKg = 25 // Below 30kg
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("WeightKg.Value")
            .WithErrorMessage("Weight must be between 30kg and 300kg");
    }

    [Fact]
    public void Should_Fail_When_WeightKg_Is_Above_Maximum()
    {
        // Arrange
        var request = new UpdateProgressRequest
        {
            RecordedValue = 65.0m,
            WeightKg = 350 // Above 300kg
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("WeightKg.Value")
            .WithErrorMessage("Weight must be between 30kg and 300kg");
    }

    [Fact]
    public void Should_Pass_When_WeightKg_Is_Valid()
    {
        // Arrange
        var request = new UpdateProgressRequest
        {
            RecordedValue = 65.0m,
            WeightKg = 70
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WeightKg);
    }

    [Fact]
    public void Should_Fail_When_WaistCm_Is_Below_Minimum()
    {
        // Arrange
        var request = new UpdateProgressRequest
        {
            RecordedValue = 65.0m,
            WaistCm = 40 // Below 50cm
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("WaistCm.Value")
            .WithErrorMessage("Waist measurement must be between 50cm and 200cm");
    }

    [Fact]
    public void Should_Fail_When_WaistCm_Is_Above_Maximum()
    {
        // Arrange
        var request = new UpdateProgressRequest
        {
            RecordedValue = 65.0m,
            WaistCm = 250 // Above 200cm
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("WaistCm.Value")
            .WithErrorMessage("Waist measurement must be between 50cm and 200cm");
    }

    [Fact]
    public void Should_Pass_When_WaistCm_Is_Valid()
    {
        // Arrange
        var request = new UpdateProgressRequest
        {
            RecordedValue = 65.0m,
            WaistCm = 80
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WaistCm);
    }

    [Fact]
    public void Should_Fail_When_ChestCm_Is_Below_Minimum()
    {
        // Arrange
        var request = new UpdateProgressRequest
        {
            RecordedValue = 65.0m,
            ChestCm = 50 // Below 60cm
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("ChestCm.Value")
            .WithErrorMessage("Chest measurement must be between 60cm and 150cm");
    }

    [Fact]
    public void Should_Fail_When_ChestCm_Is_Above_Maximum()
    {
        // Arrange
        var request = new UpdateProgressRequest
        {
            RecordedValue = 65.0m,
            ChestCm = 160 // Above 150cm
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("ChestCm.Value")
            .WithErrorMessage("Chest measurement must be between 60cm and 150cm");
    }

    [Fact]
    public void Should_Pass_When_ChestCm_Is_Valid()
    {
        // Arrange
        var request = new UpdateProgressRequest
        {
            RecordedValue = 65.0m,
            ChestCm = 95
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ChestCm);
    }

    [Fact]
    public void Should_Fail_When_HipCm_Is_Below_Minimum()
    {
        // Arrange
        var request = new UpdateProgressRequest
        {
            RecordedValue = 65.0m,
            HipCm = 60 // Below 70cm
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("HipCm.Value")
            .WithErrorMessage("Hip measurement must be between 70cm and 150cm");
    }

    [Fact]
    public void Should_Fail_When_HipCm_Is_Above_Maximum()
    {
        // Arrange
        var request = new UpdateProgressRequest
        {
            RecordedValue = 65.0m,
            HipCm = 160 // Above 150cm
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor("HipCm.Value")
            .WithErrorMessage("Hip measurement must be between 70cm and 150cm");
    }

    [Fact]
    public void Should_Pass_When_HipCm_Is_Valid()
    {
        // Arrange
        var request = new UpdateProgressRequest
        {
            RecordedValue = 65.0m,
            HipCm = 95
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.HipCm);
    }

    [Fact]
    public void Should_Fail_When_Notes_Exceed_MaxLength()
    {
        // Arrange
        var request = new UpdateProgressRequest
        {
            RecordedValue = 65.0m,
            Notes = new string('A', 501) // 501 characters
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Notes)
            .WithErrorMessage("Notes cannot exceed 500 characters");
    }

    [Fact]
    public void Should_Pass_When_Notes_Is_Valid_Length()
    {
        // Arrange
        var request = new UpdateProgressRequest
        {
            RecordedValue = 65.0m,
            Notes = new string('A', 500) // Exactly 500 characters
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void Should_Pass_When_Notes_Is_Null()
    {
        // Arrange
        var request = new UpdateProgressRequest
        {
            RecordedValue = 65.0m,
            Notes = null
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void Should_Pass_When_Notes_Is_Empty()
    {
        // Arrange
        var request = new UpdateProgressRequest
        {
            RecordedValue = 65.0m,
            Notes = ""
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Notes);
    }
}

