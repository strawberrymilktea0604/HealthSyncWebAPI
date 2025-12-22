using FluentValidation;
using HealthSync.Application.DTOs.Nutrition;

namespace HealthSync.Application.Validators.Nutrition;

public class UpdateNutritionLogNotesRequestValidator : AbstractValidator<UpdateNutritionLogNotesRequest>
{
    public UpdateNutritionLogNotesRequestValidator()
    {
        RuleFor(x => x.Notes)
            .NotEmpty()
            .WithMessage("Notes is required");
    }
}