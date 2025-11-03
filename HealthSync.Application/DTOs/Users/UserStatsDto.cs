namespace HealthSync.Application.DTOs.Users;

public record UserStatsDto(
    int TotalWorkouts,
    int TotalNutritionLogs,
    int TotalGoals,
    int TotalChallenges,
    int ContributionPoints
);