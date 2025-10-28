using System;

namespace HealthSync.Domain.Entities;

public class UserChallengeSubmission
{
    public int SubmissionId { get; set; }

    // Challenge and User references
    public int CommunityChallengeId { get; set; }
    public CommunityChallenge CommunityChallenge { get; set; } = null!;

    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    // Submission details
    public DateTime JoinedDate { get; set; }
    public DateTime? SubmissionDate { get; set; }
    public string Status { get; set; } = null!; // Joined, InProgress, Submitted, Completed, Failed, Withdrawn

    // Progress tracking
    public decimal? CurrentProgress { get; set; } // Current value (steps, calories, workouts, etc.)
    public decimal? TargetProgress { get; set; } // Target value for this user (might differ from challenge target)
    public decimal? ProgressPercentage { get; set; } // Calculated percentage

    // Submission evidence
    public string? SubmissionText { get; set; } // User's description or notes
    public string? EvidenceImageUrl { get; set; } // Photo proof
    public string? EvidenceVideoUrl { get; set; } // Video proof
    public string? AdditionalData { get; set; } // JSON data for metrics, workout logs, etc.

    // Review and verification
    public bool RequiresReview { get; set; } = false;
    public string? ReviewStatus { get; set; } // Pending, Approved, Rejected, NotRequired
    public int? ReviewedByUserId { get; set; }
    public ApplicationUser? ReviewedByUser { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }

    // Achievements and rewards
    public bool IsCompleted { get; set; } = false;
    public DateTime? CompletedAt { get; set; }
    public decimal? EarnedPoints { get; set; }
    public int? RankPosition { get; set; } // Ranking within the challenge

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}