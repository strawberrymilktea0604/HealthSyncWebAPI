namespace HealthSync.Application.DTOs.Leaderboard;

public class LeaderboardUserDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int ContributionPoints { get; set; }
}