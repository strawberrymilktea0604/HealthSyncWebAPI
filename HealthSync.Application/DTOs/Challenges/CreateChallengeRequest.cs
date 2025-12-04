using HealthSync.Domain.Entities;

namespace HealthSync.Application.DTOs.Challenges;

public class CreateChallengeRequest
{
    /// <summary>
    /// Challenge title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Challenge description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Challenge type (Workout, Nutrition, Hybrid)
    /// </summary>
    public ChallengeType ChallengeType { get; set; }

    /// <summary>
    /// Challenge start date (UTC)
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Challenge end date (UTC)
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Criteria for completion (e.g., "Run 50km in 30 days")
    /// </summary>
    public string Criteria { get; set; } = string.Empty;

    /// <summary>
    /// Max number of participants (null = unlimited)
    /// </summary>
    public int? MaxParticipants { get; set; }

    /// <summary>
    /// Reward description (e.g., "Certificate + 20% discount")
    /// </summary>
    public string? RewardDescription { get; set; }
}
