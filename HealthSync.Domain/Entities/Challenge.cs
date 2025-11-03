using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HealthSync.Domain.Entities;

public class Challenge
{
    [Key]
    public int ChallengeId { get; set; }

    // Challenge info
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public ChallengeType ChallengeType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Criteria { get; set; } = null!;
    public ChallengeStatus Status { get; set; }
    public int? MaxParticipants { get; set; }
    public string? RewardDescription { get; set; }
    public string? ImageUrl { get; set; }

    // Admin tracking
    public int CreatedByAdminId { get; set; }
    public ApplicationUser CreatedByAdmin { get; set; } = null!;

    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<ChallengeParticipation> Participations { get; set; } = new List<ChallengeParticipation>();
}