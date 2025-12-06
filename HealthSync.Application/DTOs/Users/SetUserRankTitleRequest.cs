namespace HealthSync.Application.DTOs.Users;

/// <summary>
/// DTO for setting user rank title
/// </summary>
public class SetUserRankTitleRequest
{
    /// <summary>
    /// Rank title to assign to user (e.g., "Top Contributor", "Rising Star")
    /// Can be null to clear the title
    /// </summary>
    public string? RankTitle { get; set; }
}

/// <summary>
/// DTO for user rank title response
/// </summary>
public class UserRankTitleDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// User full name
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Assigned rank title
    /// </summary>
    public string? RankTitle { get; set; }

    /// <summary>
    /// Leaderboard total points
    /// </summary>
    public int TotalPoints { get; set; }

    /// <summary>
    /// Leaderboard rank position
    /// </summary>
    public int? RankPosition { get; set; }

    /// <summary>
    /// Timestamp when title was assigned
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
