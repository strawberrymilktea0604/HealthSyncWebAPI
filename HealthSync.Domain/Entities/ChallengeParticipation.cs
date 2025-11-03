using System;
using System.ComponentModel.DataAnnotations;

namespace HealthSync.Domain.Entities;

public class ChallengeParticipation
{
    [Key]
    public int ParticipationId { get; set; }

    // Challenge reference
    public int ChallengeId { get; set; }
    public Challenge Challenge { get; set; } = null!;

    // User reference
    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    // Participation details
    public DateTime JoinedDate { get; set; }
    public ParticipationStatus Status { get; set; }

    // Submission
    public string? SubmissionText { get; set; }
    public string? SubmissionUrl { get; set; }
    public DateTime? SubmittedAt { get; set; }

    // Review
    public int? ReviewedByAdminId { get; set; }
    public ApplicationUser? ReviewedByAdmin { get; set; }
    public DateTime? ReviewDate { get; set; }
    public string? ReviewNotes { get; set; }

    // Completion
    public DateTime? CompletedAt { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; }
}