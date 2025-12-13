namespace HealthSync.Application.DTOs.Dashboard;

/// <summary>
/// DTO for user contribution ranking
/// </summary>
public class UserContributionDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// User full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// User email
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contribution points
    /// </summary>
    public int ContributionPoints { get; set; }

    /// <summary>
    /// User role
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Is user active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Rank title (if any)
    /// </summary>
    public string? RankTitle { get; set; }
}