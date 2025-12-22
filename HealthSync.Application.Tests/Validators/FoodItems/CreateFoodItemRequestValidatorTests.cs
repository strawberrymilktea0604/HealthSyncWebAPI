using FluentValidation.TestHelper;
using HealthSync.Application.DTOs.FoodItems;
using HealthSync.Application.Validators.FoodItems;
using Xunit;

namespace HealthSync.Application.Tests.Validators.FoodItems;

public class CreateFoodItemRequestValidatorTests
{
    private readonly CreateFoodItemRequestValidator _validator;

    public CreateFoodItemRequestValidatorTests()
    {
        _validator = new CreateFoodItemRequestValidator();
    }

    [Fact]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        // Arrange
        var request = new CreateFoodItemRequest
        {
            Name = "",
            Category = "Protein",
            ServingSize = 100,
            ServingUnit = "Gram",
            CaloriesPerServing = 165,
            ProteinG = 31,
            CarbsG = 0,
            FatG = 3.6m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name is required");
    }

    [Fact]
    public void Should_Have_Error_When_Name_Exceeds_MaxLength()
    {
        // Arrange
        var longName = new string('a', 201);
        var request = new CreateFoodItemRequest
        {
            Name = longName,
            Category = "Protein",
            ServingSize = 100,
            ServingUnit = "Gram",
            CaloriesPerServing = 165,
            ProteinG = 31,
            CarbsG = 0,
            FatG = 3.6m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name cannot exceed 200 characters");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Name_Is_Valid()
    {
        // Arrange
        var request = new CreateFoodItemRequest
        {
            Name = "Grilled Chicken Breast",
            Category = "Protein",
            ServingSize = 100,
            ServingUnit = "Gram",
            CaloriesPerServing = 165,
            ProteinG = 31,
            CarbsG = 0,
            FatG = 3.6m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Should_Have_Error_When_Category_Is_Invalid()
    {
        // Arrange
        var request = new CreateFoodItemRequest
        {
            Name = "Grilled Chicken Breast",
            Category = "InvalidCategory",
            ServingSize = 100,
            ServingUnit = "Gram",
            CaloriesPerServing = 165,
            ProteinG = 31,
            CarbsG = 0,
            FatG = 3.6m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Category)
            .WithErrorMessage("Invalid food category");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Category_Is_Valid()
    {
        // Arrange
        var request = new CreateFoodItemRequest
        {
            Name = "Grilled Chicken Breast",
            Category = "Protein",
            ServingSize = 100,
            ServingUnit = "Gram",
            CaloriesPerServing = 165,
            ProteinG = 31,
            CarbsG = 0,
            FatG = 3.6m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Category);
    }

    [Fact]
    public void Should_Have_Error_When_Description_Exceeds_MaxLength()
    {
        // Arrange
        var longDescription = new string('a', 1001);
        var request = new CreateFoodItemRequest
        {
            Name = "Grilled Chicken Breast",
            Category = "Protein",
            ServingSize = 100,
            ServingUnit = "Gram",
            CaloriesPerServing = 165,
            ProteinG = 31,
            CarbsG = 0,
            FatG = 3.6m,
            Description = longDescription
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 1000 characters");
    }

    [Fact]
    public void Should_Have_Error_When_ImageUrl_Exceeds_MaxLength()
    {
        // Arrange
        var longUrl = new string('a', 501);
        var request = new CreateFoodItemRequest
        {
            Name = "Grilled Chicken Breast",
            Category = "Protein",
            ServingSize = 100,
            ServingUnit = "Gram",
            CaloriesPerServing = 165,
            ProteinG = 31,
            CarbsG = 0,
            FatG = 3.6m,
            ImageUrl = longUrl
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ImageUrl)
            .WithErrorMessage("Image URL cannot exceed 500 characters");
    }

    [Fact]
    public void Should_Have_Error_When_ServingSize_Is_Zero()
    {
        // Arrange
        var request = new CreateFoodItemRequest
        {
            Name = "Grilled Chicken Breast",
            Category = "Protein",
            ServingSize = 0,
            ServingUnit = "Gram",
            CaloriesPerServing = 165,
            ProteinG = 31,
            CarbsG = 0,
            FatG = 3.6m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ServingSize)
            .WithErrorMessage("Serving size must be greater than 0");
    }

    [Fact]
    public void Should_Have_Error_When_ServingSize_Is_Negative()
    {
        // Arrange
        var request = new CreateFoodItemRequest
        {
            Name = "Grilled Chicken Breast",
            Category = "Protein",
            ServingSize = -1,
            ServingUnit = "Gram",
            CaloriesPerServing = 165,
            ProteinG = 31,
            CarbsG = 0,
            FatG = 3.6m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ServingSize)
            .WithErrorMessage("Serving size must be greater than 0");
    }

    [Fact]
    public void Should_Have_Error_When_ServingUnit_Is_Invalid()
    {
        // Arrange
        var request = new CreateFoodItemRequest
        {
            Name = "Grilled Chicken Breast",
            Category = "Protein",
            ServingSize = 100,
            ServingUnit = "InvalidUnit",
            CaloriesPerServing = 165,
            ProteinG = 31,
            CarbsG = 0,
            FatG = 3.6m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ServingUnit)
            .WithErrorMessage("Invalid serving unit");
    }

    [Fact]
    public void Should_Not_Have_Error_When_ServingUnit_Is_Valid()
    {
        // Arrange
        var request = new CreateFoodItemRequest
        {
            Name = "Grilled Chicken Breast",
            Category = "Protein",
            ServingSize = 100,
            ServingUnit = "Gram",
            CaloriesPerServing = 165,
            ProteinG = 31,
            CarbsG = 0,
            FatG = 3.6m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ServingUnit);
    }

    [Fact]
    public void Should_Have_Error_When_CaloriesPerServing_Is_Negative()
    {
        // Arrange
        var request = new CreateFoodItemRequest
        {
            Name = "Grilled Chicken Breast",
            Category = "Protein",
            ServingSize = 100,
            ServingUnit = "Gram",
            CaloriesPerServing = -1,
            ProteinG = 31,
            CarbsG = 0,
            FatG = 3.6m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CaloriesPerServing)
            .WithErrorMessage("Calories per serving must be 0 or greater");
    }

    [Fact]
    public void Should_Not_Have_Error_When_CaloriesPerServing_Is_Zero()
    {
        // Arrange
        var request = new CreateFoodItemRequest
        {
            Name = "Grilled Chicken Breast",
            Category = "Protein",
            ServingSize = 100,
            ServingUnit = "Gram",
            CaloriesPerServing = 0,
            ProteinG = 31,
            CarbsG = 0,
            FatG = 3.6m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CaloriesPerServing);
    }

    [Fact]
    public void Should_Have_Error_When_ProteinG_Is_Negative()
    {
        // Arrange
        var request = new CreateFoodItemRequest
        {
            Name = "Grilled Chicken Breast",
            Category = "Protein",
            ServingSize = 100,
            ServingUnit = "Gram",
            CaloriesPerServing = 165,
            ProteinG = -1,
            CarbsG = 0,
            FatG = 3.6m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProteinG)
            .WithErrorMessage("Protein must be 0 or greater");
    }

    [Fact]
    public void Should_Have_Error_When_CarbsG_Is_Negative()
    {
        // Arrange
        var request = new CreateFoodItemRequest
        {
            Name = "Grilled Chicken Breast",
            Category = "Protein",
            ServingSize = 100,
            ServingUnit = "Gram",
            CaloriesPerServing = 165,
            ProteinG = 31,
            CarbsG = -1,
            FatG = 3.6m
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CarbsG)
            .WithErrorMessage("Carbs must be 0 or greater");
    }

    [Fact]
    public void Should_Have_Error_When_FatG_Is_Negative()
    {
        // Arrange
        var request = new CreateFoodItemRequest
        {
            Name = "Grilled Chicken Breast",
            Category = "Protein",
            ServingSize = 100,
            ServingUnit = "Gram",
            CaloriesPerServing = 165,
            ProteinG = 31,
            CarbsG = 0,
            FatG = -1
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FatG)
            .WithErrorMessage("Fat must be 0 or greater");
    }

    [Fact]
    public void Should_Have_Error_When_FiberG_Is_Negative()
    {
        // Arrange
        var request = new CreateFoodItemRequest
        {
            Name = "Grilled Chicken Breast",
            Category = "Protein",
            ServingSize = 100,
            ServingUnit = "Gram",
            CaloriesPerServing = 165,
            ProteinG = 31,
            CarbsG = 0,
            FatG = 3.6m,
            FiberG = -1
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FiberG)
            .WithErrorMessage("Fiber must be 0 or greater");
    }

    [Fact]
    public void Should_Have_Error_When_SugarG_Is_Negative()
    {
        // Arrange
        var request = new CreateFoodItemRequest
        {
            Name = "Grilled Chicken Breast",
            Category = "Protein",
            ServingSize = 100,
            ServingUnit = "Gram",
            CaloriesPerServing = 165,
            ProteinG = 31,
            CarbsG = 0,
            FatG = 3.6m,
            SugarG = -1
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SugarG)
            .WithErrorMessage("Sugar must be 0 or greater");
    }

    [Fact]
    public void Should_Not_Have_Error_When_All_Fields_Are_Valid()
    {
        // Arrange
        var request = new CreateFoodItemRequest
        {
            Name = "Grilled Chicken Breast",
            Category = "Protein",
            ServingSize = 100,
            ServingUnit = "Gram",
            CaloriesPerServing = 165,
            ProteinG = 31,
            CarbsG = 0,
            FatG = 3.6m,
            FiberG = 0,
            SugarG = 0,
            Description = "Lean protein source",
            ImageUrl = "https://example.com/chicken.jpg"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

