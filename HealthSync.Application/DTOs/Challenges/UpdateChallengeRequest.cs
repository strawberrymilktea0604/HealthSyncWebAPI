using HealthSync.Domain.Entities;

namespace HealthSync.Application.DTOs.Challenges;

public class UpdateChallengeRequest
{
    /// <summary>
    /// Challenge title
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Challenge description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Challenge status
    /// </summary>
    public ChallengeStatus? Status { get; set; }

    /// <summary>
    /// Max number of participants
    /// </summary>
    public int? MaxParticipants { get; set; }

    /// <summary>
    /// Reward description
    /// </summary>
    public string? RewardDescription { get; set; }
}
