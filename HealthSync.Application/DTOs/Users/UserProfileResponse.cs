namespace HealthSync.Application.DTOs.Users;

public record UserProfileResponse(
    int UserProfileId,
    string FullName,
    string? Gender,
    DateTime? DateOfBirth,
    decimal? HeightCm,
    decimal? CurrentWeightKg,
    string? ActivityLevel,
    string? AvatarUrl,
    int ContributionPoints,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);