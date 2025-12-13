namespace HealthSync.Application.DTOs.Leaderboard;

public class UserRankDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int TotalPoints { get; set; }
    public string? RankTitle { get; set; }
    public int RankPosition { get; set; }
    public DateTime UpdatedAt { get; set; }
}