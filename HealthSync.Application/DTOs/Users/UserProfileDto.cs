using System.ComponentModel.DataAnnotations;

namespace HealthSync.Application.DTOs.Users;

public record UserProfileDto(
    int UserProfileId,
    int UserId,
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