using FluentValidation;
using HealthSync.Application.DTOs.Exercises;
using HealthSync.Domain.Entities;

namespace HealthSync.Application.Validators.Exercises;

public class CreateExerciseRequestValidator : AbstractValidator<CreateExerciseRequest>
{
    public CreateExerciseRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Exercise name is required")
            .MaximumLength(100).WithMessage("Exercise name must not exceed 100 characters");

        RuleFor(x => x.MuscleGroup)
            .NotEmpty().WithMessage("Muscle group is required")
            .Must(BeValidMuscleGroup).WithMessage("Invalid muscle group");

        RuleFor(x => x.Difficulty)
            .NotEmpty().WithMessage("Difficulty level is required")
            .Must(BeValidDifficulty).WithMessage("Invalid difficulty level");

        RuleFor(x => x.Equipment)
            .Must(BeValidEquipment).WithMessage("Invalid equipment")
            .When(x => !string.IsNullOrEmpty(x.Equipment));

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.Instructions)
            .MaximumLength(2000).WithMessage("Instructions must not exceed 2000 characters");

        RuleFor(x => x.ImageUrl)
            .Must(BeValidUrl).WithMessage("Invalid image URL")
            .When(x => !string.IsNullOrEmpty(x.ImageUrl));

        RuleFor(x => x.VideoUrl)
            .Must(BeValidUrl).WithMessage("Invalid video URL")
            .When(x => !string.IsNullOrEmpty(x.VideoUrl));

        RuleFor(x => x.CaloriesPerMinute)
            .GreaterThan(0).WithMessage("Calories per minute must be greater than 0")
            .When(x => x.CaloriesPerMinute.HasValue);
    }

    private bool BeValidMuscleGroup(string muscleGroup)
    {
        return Enum.TryParse<MuscleGroup>(muscleGroup, true, out _);
    }

    private bool BeValidDifficulty(string difficulty)
    {
        return Enum.TryParse<DifficultyLevel>(difficulty, true, out _);
    }

    private bool BeValidEquipment(string equipment)
    {
        return Enum.TryParse<Equipment>(equipment, true, out _);
    }

    private bool BeValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}