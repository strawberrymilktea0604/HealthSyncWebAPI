using FluentValidation;
using HealthSync.Application.DTOs.FoodItems;

namespace HealthSync.Application.Validators.FoodItems;

public class CreateFoodItemRequestValidator : AbstractValidator<CreateFoodItemRequest>
{
    public CreateFoodItemRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(250);
        RuleFor(x => x.ServingSize).GreaterThan(0);
        RuleFor(x => x.ServingUnit).NotEmpty().MaximumLength(50);
        RuleFor(x => x.CaloriesPerServing).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ProteinG).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CarbsG).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FatG).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => !string.IsNullOrEmpty(x.Description));
    }
}