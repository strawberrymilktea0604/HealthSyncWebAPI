namespace HealthSync.Domain.Entities;

public class Leaderboard
{
    public int LeaderboardId { get; set; }
    public int UserId { get; set; }
    public int TotalPoints { get; set; }
    public string? RankTitle { get; set; }

    // Navigation property
    public ApplicationUser User { get; set; } = null!;
}