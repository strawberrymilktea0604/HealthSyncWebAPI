using FluentValidation;
using HealthSync.Application.DTOs.Nutrition;

namespace HealthSync.Application.Validators.Nutrition;

public class CreateNutritionLogRequestValidator : AbstractValidator<CreateNutritionLogRequest>
{
    public CreateNutritionLogRequestValidator()
    {
        RuleFor(x => x.LogDate)
            .NotNull()
            .WithMessage("LogDate is required");

        RuleFor(x => x.FoodEntries)
            .NotNull()
            .WithMessage("FoodEntries is required");
    }
}