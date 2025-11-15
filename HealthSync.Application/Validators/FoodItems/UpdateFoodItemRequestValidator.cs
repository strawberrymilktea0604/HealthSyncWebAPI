using FluentValidation;
using HealthSync.Application.DTOs.FoodItems;

namespace HealthSync.Application.Validators.FoodItems;

public class UpdateFoodItemRequestValidator : AbstractValidator<UpdateFoodItemRequest>
{
    public UpdateFoodItemRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Invalid food category");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500).WithMessage("Image URL cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.ImageUrl));

        RuleFor(x => x.ServingSize)
            .GreaterThan(0).WithMessage("Serving size must be greater than 0");

        RuleFor(x => x.ServingUnit)
            .IsInEnum().WithMessage("Invalid serving unit");

        RuleFor(x => x.CaloriesPerServing)
            .GreaterThanOrEqualTo(0).WithMessage("Calories per serving must be 0 or greater");

        RuleFor(x => x.ProteinG)
            .GreaterThanOrEqualTo(0).WithMessage("Protein must be 0 or greater");

        RuleFor(x => x.CarbsG)
            .GreaterThanOrEqualTo(0).WithMessage("Carbs must be 0 or greater");

        RuleFor(x => x.FatG)
            .GreaterThanOrEqualTo(0).WithMessage("Fat must be 0 or greater");

        RuleFor(x => x.FiberG)
            .GreaterThanOrEqualTo(0).WithMessage("Fiber must be 0 or greater")
            .When(x => x.FiberG.HasValue);

        RuleFor(x => x.SugarG)
            .GreaterThanOrEqualTo(0).WithMessage("Sugar must be 0 or greater")
            .When(x => x.SugarG.HasValue);
    }
}
