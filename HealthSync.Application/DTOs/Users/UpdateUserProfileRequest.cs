namespace HealthSync.Application.DTOs.Users;

public record UpdateUserProfileRequest(
    string FullName,
    string? Gender,
    DateTime? DateOfBirth,
    decimal? HeightCm,
    decimal? CurrentWeightKg,
    string? ActivityLevel,
    string? AvatarUrl
);