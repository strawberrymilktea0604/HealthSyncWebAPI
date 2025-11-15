using FluentValidation;
using HealthSync.Application.DTOs.Goals;

namespace HealthSync.Application.Validators.Goals;

public class UpdateProgressValidator : AbstractValidator<UpdateProgressRequest>
{
    public UpdateProgressValidator()
    {
        RuleFor(x => x.RecordedValue)
            .GreaterThan(0)
            .When(x => x.RecordedValue.HasValue)
            .WithMessage("Recorded value must be greater than 0");

        When(x => x.WeightKg.HasValue, () =>
        {
            RuleFor(x => x.WeightKg.Value)
                .InclusiveBetween(30, 300)
                .WithMessage("Weight must be between 30kg and 300kg");
        });

        When(x => x.WaistCm.HasValue, () =>
        {
            RuleFor(x => x.WaistCm.Value)
                .InclusiveBetween(50, 200)
                .WithMessage("Waist measurement must be between 50cm and 200cm");
        });

        When(x => x.ChestCm.HasValue, () =>
        {
            RuleFor(x => x.ChestCm.Value)
                .InclusiveBetween(60, 150)
                .WithMessage("Chest measurement must be between 60cm and 150cm");
        });

        When(x => x.HipCm.HasValue, () =>
        {
            RuleFor(x => x.HipCm.Value)
                .InclusiveBetween(70, 150)
                .WithMessage("Hip measurement must be between 70cm and 150cm");
        });

        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Notes))
            .WithMessage("Notes cannot exceed 500 characters");
    }
}