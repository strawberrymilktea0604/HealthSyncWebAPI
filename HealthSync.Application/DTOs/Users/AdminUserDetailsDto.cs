using HealthSync.Application.DTOs.Users;

namespace HealthSync.Application.DTOs.Users;

public record AdminUserDetailsDto(
    AdminUserDto User,
    UserProfileDto Profile,
    UserStatsDto Stats
);