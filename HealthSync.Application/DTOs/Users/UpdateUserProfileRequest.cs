namespace HealthSync.Application.DTOs.Users;

public record UpdateUserProfileRequest(
    string FullName,
    string? Gender,
    DateTime? DateOfBirth,
    decimal? CurrentHeightCm,
    decimal? CurrentWeightKg,
    string? AvatarUrl
);