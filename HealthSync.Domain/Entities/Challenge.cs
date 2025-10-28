using System;
using System.Collections.Generic;

namespace HealthSync.Domain.Entities;

public class Challenge
{
    public int ChallengeId { get; set; }

    // Challenge info
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = null!; // Open, Closed

    // Navigation properties
    public ICollection<ChallengeParticipation> Participations { get; set; } = new List<ChallengeParticipation>();
}