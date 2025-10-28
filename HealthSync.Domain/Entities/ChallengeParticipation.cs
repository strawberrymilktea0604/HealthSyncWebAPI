using System;

namespace HealthSync.Domain.Entities;

public class ChallengeParticipation
{
    public int ParticipationId { get; set; }

    // Challenge reference
    public int ChallengeId { get; set; }
    public Challenge Challenge { get; set; } = null!;

    // User reference
    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    // Participation details
    public DateTime JoinedDate { get; set; }
    public string Status { get; set; } = null!; // Joined, Pending, Completed, Failed
    public string? SubmissionUrl { get; set; }
    public int? ReviewedBy { get; set; }
    public DateTime? ReviewDate { get; set; }
}