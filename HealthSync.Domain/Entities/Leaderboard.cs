namespace HealthSync.Domain.Entities;

public class Leaderboard
{
    public int LeaderboardId { get; set; }
    public int UserId { get; set; }
    public int TotalPoints { get; set; }
    public string? RankTitle { get; set; }
    public int? RankPosition { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ApplicationUser User { get; set; } = null!;
}