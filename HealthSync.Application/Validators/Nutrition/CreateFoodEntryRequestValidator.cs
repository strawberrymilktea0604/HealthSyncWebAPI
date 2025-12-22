using FluentValidation;
using HealthSync.Application.DTOs.Nutrition;

namespace HealthSync.Application.Validators.Nutrition;

public class CreateFoodEntryRequestValidator : AbstractValidator<CreateFoodEntryRequest>
{
    public CreateFoodEntryRequestValidator()
    {
        RuleFor(x => x.FoodItemId)
            .GreaterThan(0)
            .WithMessage("FoodItemId must be greater than 0");

        RuleFor(x => x.MealType)
            .NotEmpty()
            .WithMessage("MealType is required")
            .Must(x => x == "Breakfast" || x == "Lunch" || x == "Dinner" || x == "Snack")
            .WithMessage("MealType must be Breakfast, Lunch, Dinner, or Snack");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0");
    }
}