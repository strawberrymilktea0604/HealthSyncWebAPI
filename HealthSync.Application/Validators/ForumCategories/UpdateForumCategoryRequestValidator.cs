using FluentValidation;
using HealthSync.Application.DTOs.Forum;

namespace HealthSync.Application.DTOs.Validators.ForumCategories;

public class UpdateForumCategoryRequestValidator : AbstractValidator<UpdateForumCategoryRequest>
{
    public UpdateForumCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required")
            .Length(2, 100).WithMessage("Category name must be between 2 and 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Display order must be greater than or equal to 0");
    }
}