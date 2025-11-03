using System.ComponentModel.DataAnnotations;

namespace HealthSync.Application.DTOs.Users;

public record UserProfileDto(
    int UserProfileId,
    int UserId,
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