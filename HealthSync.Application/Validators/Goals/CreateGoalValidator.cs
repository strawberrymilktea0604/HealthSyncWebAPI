using FluentValidation;
using HealthSync.Application.DTOs.Goals;
using HealthSync.Domain.Entities;

namespace HealthSync.Application.Validators.Goals;

public class CreateGoalValidator : AbstractValidator<CreateGoalRequest>
{
    public CreateGoalValidator()
    {
        RuleFor(x => x.GoalType)
            .IsInEnum()
            .WithMessage("Invalid goal type");

        RuleFor(x => x.TargetValue)
            .GreaterThan(0)
            .WithMessage("Target value must be greater than 0");

        RuleFor(x => x.Unit)
            .NotEmpty()
            .MaximumLength(10)
            .WithMessage("Unit is required and must not exceed 10 characters");

        RuleFor(x => x.StartDate)
            .NotEmpty()
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("Start date must be today or in the future");

        RuleFor(x => x.EndDate)
            .NotEmpty()
            .GreaterThan(x => x.StartDate)
            .WithMessage("End date must be after start date");

        RuleFor(x => x)
            .Must(HaveReasonableDuration)
            .WithMessage("Goal duration must be between 1 week and 1 year");

        When(x => x.GoalType == GoalType.BodyMeasurement, () =>
        {
            RuleFor(x => x.Unit)
                .Must(unit => unit == "cm")
                .WithMessage("Body measurement goals must use 'cm' as unit");
        });

        When(x => x.GoalType == GoalType.WeightLoss || x.GoalType == GoalType.WeightGain || x.GoalType == GoalType.MaintainWeight, () =>
        {
            RuleFor(x => x.Unit)
                .Must(unit => unit == "kg")
                .WithMessage("Weight goals must use 'kg' as unit");
        });
    }

    private bool HaveReasonableDuration(CreateGoalRequest request)
    {
        var duration = request.EndDate - request.StartDate;
        return duration.TotalDays >= 7 && duration.TotalDays <= 365;
    }
}