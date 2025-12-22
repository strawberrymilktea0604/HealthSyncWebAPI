using FluentValidation.TestHelper;
using HealthSync.Application.DTOs.ForumCategories;
using HealthSync.Application.Validators.ForumCategories;
using Xunit;

namespace HealthSync.Application.Tests.Validators.ForumCategories;

public class UpdateForumCategoryRequestValidatorTests
{
    private readonly UpdateForumCategoryRequestValidator _validator;

    public UpdateForumCategoryRequestValidatorTests()
    {
        _validator = new UpdateForumCategoryRequestValidator();
    }

    [Fact]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        // Arrange
        var request = new UpdateForumCategoryRequest
        {
            Name = "",
            Description = "General discussion"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Category name is required");
    }

    [Fact]
    public void Should_Have_Error_When_Name_Is_Too_Short()
    {
        // Arrange
        var request = new UpdateForumCategoryRequest
        {
            Name = "A",
            Description = "General discussion"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Category name must be between 2 and 100 characters");
    }

    [Fact]
    public void Should_Have_Error_When_Name_Is_Too_Long()
    {
        // Arrange
        var longName = new string('a', 101);
        var request = new UpdateForumCategoryRequest
        {
            Name = longName,
            Description = "General discussion"
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Category name must be between 2 and 100 characters");
    }

    [Fact]
    public void Should_Have_Error_When_Description_Exceeds_MaxLength()
    {
        // Arrange
        var longDescription = new string('a', 501);
        var request = new UpdateForumCategoryRequest
        {
            Name = "General Discussion",
            Description = longDescription
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 500 characters");
    }

    [Fact]
    public void Should_Have_Error_When_DisplayOrder_Is_Negative()
    {
        // Arrange
        var request = new UpdateForumCategoryRequest
        {
            Name = "General Discussion",
            Description = "General discussion",
            DisplayOrder = -1
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DisplayOrder)
            .WithErrorMessage("Display order must be greater than or equal to 0");
    }

    [Fact]
    public void Should_Not_Have_Error_When_All_Fields_Are_Valid()
    {
        // Arrange
        var request = new UpdateForumCategoryRequest
        {
            Name = "General Discussion",
            Description = "General discussion about health and fitness",
            DisplayOrder = 1
        };

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

