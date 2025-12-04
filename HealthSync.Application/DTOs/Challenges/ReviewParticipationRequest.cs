namespace HealthSync.Application.DTOs.Challenges;

public class ReviewParticipationRequest
{
    /// <summary>
    /// Whether the submission is approved
    /// </summary>
    public bool Approved { get; set; }

    /// <summary>
    /// Admin review notes
    /// </summary>
    public string? ReviewNotes { get; set; }
}
