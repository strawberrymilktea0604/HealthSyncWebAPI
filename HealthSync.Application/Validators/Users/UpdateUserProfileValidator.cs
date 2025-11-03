using FluentValidation;
using HealthSync.Application.DTOs.Users;

namespace HealthSync.Application.Validators.Users;

public class UpdateUserProfileValidator : AbstractValidator<UpdateUserProfileRequest>
{
    public UpdateUserProfileValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters");

        RuleFor(x => x.Gender)
            .Must(x => x == null || x is "Male" or "Female" or "Other")
            .WithMessage("Gender must be either 'Male', 'Female', 'Other' or null");

        RuleFor(x => x.DateOfBirth)
            .Must(x => x == null || x <= DateTime.UtcNow.AddYears(-13))
            .WithMessage("User must be at least 13 years old");

        RuleFor(x => x.CurrentHeightCm)
            .GreaterThanOrEqualTo(0).When(x => x.CurrentHeightCm.HasValue)
            .Must(x => x == null || x is >= 30 and <= 300)
            .WithMessage("Height must be between 30cm and 300cm");

        RuleFor(x => x.CurrentWeightKg)
            .GreaterThanOrEqualTo(0).When(x => x.CurrentWeightKg.HasValue)
            .Must(x => x == null || x is >= 20 and <= 500)
            .WithMessage("Weight must be between 20kg and 500kg");
    }
}