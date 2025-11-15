using FluentValidation;
using HealthSync.Application.DTOs.Goals;

namespace HealthSync.Application.Validators.Goals;

public class UpdateGoalValidator : AbstractValidator<UpdateGoalRequest>
{
    public UpdateGoalValidator()
    {
        RuleFor(x => x.GoalType)
            .IsInEnum()
            .WithMessage("Invalid goal type");

        RuleFor(x => x.TargetValue)
            .GreaterThan(0)
            .WithMessage("Target value must be greater than 0");

        RuleFor(x => x.Unit)
            .NotEmpty()
            .Must(unit => unit == "kg" || unit == "cm" || unit == "%")
            .WithMessage("Unit must be one of: kg, cm, %");

        RuleFor(x => x.StartDate)
            .NotEmpty()
            .WithMessage("Start date is required");

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date");
    }
}