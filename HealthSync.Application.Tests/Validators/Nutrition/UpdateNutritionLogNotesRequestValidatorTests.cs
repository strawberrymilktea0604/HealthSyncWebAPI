using FluentValidation.TestHelper;
using HealthSync.Application.DTOs.Nutrition;
using HealthSync.Application.Validators.Nutrition;
using Xunit;

namespace HealthSync.Application.Tests.Validators.Nutrition;

public class UpdateNutritionLogNotesRequestValidatorTests
{
    private readonly UpdateNutritionLogNotesRequestValidator _validator;

    public UpdateNutritionLogNotesRequestValidatorTests()
    {
        _validator = new UpdateNutritionLogNotesRequestValidator();
    }

    [Fact]
    public void Validate_ValidRequest_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new UpdateNutritionLogNotesRequest
        {
            Notes = "Had a great meal today"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_NotesIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var request = new UpdateNutritionLogNotesRequest
        {
            Notes = ""
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Notes)
            .WithErrorMessage("Notes is required");
    }

    [Fact]
    public void Validate_NotesIsNull_ShouldHaveValidationError()
    {
        // Arrange
        var request = new UpdateNutritionLogNotesRequest
        {
            Notes = null!
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Notes)
            .WithErrorMessage("Notes is required");
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Validate_NotesIsWhitespace_ShouldHaveValidationError(string notes)
    {
        // Arrange
        var request = new UpdateNutritionLogNotesRequest
        {
            Notes = notes
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Notes)
            .WithErrorMessage("Notes is required");
    }

    [Theory]
    [InlineData("Simple note")]
    [InlineData("A")]
    [InlineData("This is a longer note with multiple words and sentences. It can contain any text.")]
    public void Validate_ValidNotes_ShouldNotHaveValidationError(string notes)
    {
        // Arrange
        var request = new UpdateNutritionLogNotesRequest
        {
            Notes = notes
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void Validate_NotesWithSpecialCharacters_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new UpdateNutritionLogNotesRequest
        {
            Notes = "Note with special chars: @#$%^&*()_+-=[]{}|;:',.<>?/"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void Validate_NotesWithEmojis_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new UpdateNutritionLogNotesRequest
        {
            Notes = "Great meal today! 🍕🍔🍟"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Notes);
    }
}
