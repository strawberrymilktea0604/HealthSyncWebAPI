using FluentValidation.TestHelper;
using HealthSync.Application.DTOs.Nutrition;
using HealthSync.Application.Validators.Nutrition;
using Xunit;

namespace HealthSync.Application.Tests.Validators.Nutrition;

public class CreateNutritionLogRequestValidatorTests
{
    private readonly CreateNutritionLogRequestValidator _validator;

    public CreateNutritionLogRequestValidatorTests()
    {
        _validator = new CreateNutritionLogRequestValidator();
    }

    [Fact]
    public void Validate_ValidRequest_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new CreateNutritionLogRequest
        {
            LogDate = DateTime.UtcNow,
            FoodEntries = new List<CreateFoodEntryRequest>()
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_LogDateIsNull_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateNutritionLogRequest
        {
            LogDate = default,
            FoodEntries = new List<CreateFoodEntryRequest>()
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LogDate)
            .WithErrorMessage("LogDate is required");
    }

    [Fact]
    public void Validate_FoodEntriesIsNull_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateNutritionLogRequest
        {
            LogDate = DateTime.UtcNow,
            FoodEntries = null!
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FoodEntries)
            .WithErrorMessage("FoodEntries is required");
    }

    [Fact]
    public void Validate_EmptyFoodEntriesList_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new CreateNutritionLogRequest
        {
            LogDate = DateTime.UtcNow,
            FoodEntries = new List<CreateFoodEntryRequest>()
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FoodEntries);
    }

    [Fact]
    public void Validate_FoodEntriesWithItems_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new CreateNutritionLogRequest
        {
            LogDate = DateTime.UtcNow,
            FoodEntries = new List<CreateFoodEntryRequest>
            {
                new CreateFoodEntryRequest { FoodItemId = 1, MealType = "Breakfast", Quantity = 1.5m }
            }
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FoodEntries);
    }

    [Fact]
    public void Validate_BothFieldsInvalid_ShouldHaveMultipleValidationErrors()
    {
        // Arrange
        var request = new CreateNutritionLogRequest
        {
            LogDate = default,
            FoodEntries = null!
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LogDate);
        result.ShouldHaveValidationErrorFor(x => x.FoodEntries);
    }

    [Theory]
    [InlineData("2023-01-01")]
    [InlineData("2024-06-15")]
    [InlineData("2025-12-31")]
    public void Validate_VariousValidDates_ShouldNotHaveValidationError(string dateString)
    {
        // Arrange
        var request = new CreateNutritionLogRequest
        {
            LogDate = DateTime.Parse(dateString),
            FoodEntries = new List<CreateFoodEntryRequest>()
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.LogDate);
    }
}
