using HealthSync.Domain.Entities;

namespace HealthSync.Application.DTOs.Challenges;

public class ChallengeDto
{
    public int ChallengeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ChallengeType ChallengeType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Criteria { get; set; } = string.Empty;
    public ChallengeStatus Status { get; set; }
    public int? MaxParticipants { get; set; }
    public int CurrentParticipants { get; set; }
    public string? RewardDescription { get; set; }
    public string? ImageUrl { get; set; }
    public int CreatedByAdminId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
