using System;
using System.Collections.Generic;

namespace HealthSync.Domain.Entities;

public class CommunityChallenge
{
    public int CommunityChallengeId { get; set; }

    // Challenge details
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? ImageUrl { get; set; }

    // Challenge period
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Challenge configuration
    public string ChallengeType { get; set; } = null!; // Workout, Nutrition, Weight Loss, Step Count, etc.
    public string? TargetMetric { get; set; } // steps, calories, workouts, etc.
    public decimal? TargetValue { get; set; }
    public string? Rules { get; set; }

    // Rewards and incentives
    public string? RewardDescription { get; set; }
    public decimal? RewardPoints { get; set; }

    // Status and visibility
    public string Status { get; set; } = null!; // Draft, Active, Completed, Cancelled
    public bool IsPublic { get; set; } = true;
    public int MaxParticipants { get; set; } = 0; // 0 = unlimited

    // Creator
    public int CreatedByUserId { get; set; }
    public ApplicationUser CreatedByUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<UserChallengeSubmission> Submissions { get; set; } = new List<UserChallengeSubmission>();
    
    // Calculated properties (can be computed from submissions)
    public int ParticipantCount { get; set; } = 0;
    public int CompletionCount { get; set; } = 0;
}