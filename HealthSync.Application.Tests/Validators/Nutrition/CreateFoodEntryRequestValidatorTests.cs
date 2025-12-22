using FluentValidation.TestHelper;
using HealthSync.Application.DTOs.Nutrition;
using HealthSync.Application.Validators.Nutrition;
using Xunit;

namespace HealthSync.Application.Tests.Validators.Nutrition;

public class CreateFoodEntryRequestValidatorTests
{
    private readonly CreateFoodEntryRequestValidator _validator;

    public CreateFoodEntryRequestValidatorTests()
    {
        _validator = new CreateFoodEntryRequestValidator();
    }

    [Fact]
    public void Validate_ValidRequest_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new CreateFoodEntryRequest
        {
            FoodItemId = 1,
            MealType = "Breakfast",
            Quantity = 1.5m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Validate_FoodItemIdLessThanOrEqualToZero_ShouldHaveValidationError(int foodItemId)
    {
        // Arrange
        var request = new CreateFoodEntryRequest
        {
            FoodItemId = foodItemId,
            MealType = "Breakfast",
            Quantity = 1.5m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FoodItemId)
            .WithErrorMessage("FoodItemId must be greater than 0");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_MealTypeEmpty_ShouldHaveValidationError(string? mealType)
    {
        // Arrange
        var request = new CreateFoodEntryRequest
        {
            FoodItemId = 1,
            MealType = mealType!,
            Quantity = 1.5m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MealType)
            .WithErrorMessage("MealType is required");
    }

    [Theory]
    [InlineData("InvalidMealType")]
    [InlineData("Brunch")]
    [InlineData("breakfast")]  // case sensitive
    [InlineData("BREAKFAST")]  // case sensitive
    public void Validate_InvalidMealType_ShouldHaveValidationError(string mealType)
    {
        // Arrange
        var request = new CreateFoodEntryRequest
        {
            FoodItemId = 1,
            MealType = mealType,
            Quantity = 1.5m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MealType)
            .WithErrorMessage("MealType must be Breakfast, Lunch, Dinner, or Snack");
    }

    [Theory]
    [InlineData("Breakfast")]
    [InlineData("Lunch")]
    [InlineData("Dinner")]
    [InlineData("Snack")]
    public void Validate_ValidMealTypes_ShouldNotHaveValidationError(string mealType)
    {
        // Arrange
        var request = new CreateFoodEntryRequest
        {
            FoodItemId = 1,
            MealType = mealType,
            Quantity = 1.5m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.MealType);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(-1)]
    [InlineData(-10.5)]
    public void Validate_QuantityLessThanOrEqualToZero_ShouldHaveValidationError(decimal quantity)
    {
        // Arrange
        var request = new CreateFoodEntryRequest
        {
            FoodItemId = 1,
            MealType = "Breakfast",
            Quantity = quantity
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Quantity)
            .WithErrorMessage("Quantity must be greater than 0");
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(1)]
    [InlineData(2.5)]
    [InlineData(10)]
    public void Validate_ValidQuantity_ShouldNotHaveValidationError(decimal quantity)
    {
        // Arrange
        var request = new CreateFoodEntryRequest
        {
            FoodItemId = 1,
            MealType = "Breakfast",
            Quantity = quantity
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Validate_MultipleInvalidFields_ShouldHaveMultipleValidationErrors()
    {
        // Arrange
        var request = new CreateFoodEntryRequest
        {
            FoodItemId = 0,
            MealType = "",
            Quantity = -1
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FoodItemId);
        result.ShouldHaveValidationErrorFor(x => x.MealType);
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }
}
