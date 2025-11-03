namespace HealthSync.Application.DTOs.Users;

public record UserProfileResponse(
    int UserProfileId,
    string FullName,
    string? Gender,
    DateTime? DateOfBirth,
    decimal? InitialHeightCm,
    decimal? InitialWeightKg,
    decimal? CurrentHeightCm,
    decimal? CurrentWeightKg,
    string? ActivityLevel,
    string? AvatarUrl,
    DateTime CreatedAt
);