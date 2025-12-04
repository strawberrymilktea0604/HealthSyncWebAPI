using HealthSync.Domain.Entities;

namespace HealthSync.Application.DTOs.Challenges;

public class ParticipationDto
{
    public int ParticipationId { get; set; }
    public int ChallengeId { get; set; }
    public int UserId { get; set; }
    public string? UserFullName { get; set; }
    public DateTime JoinedDate { get; set; }
    public ParticipationStatus Status { get; set; }
    public string? SubmissionText { get; set; }
    public string? SubmissionUrl { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public int? ReviewedByAdminId { get; set; }
    public DateTime? ReviewDate { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTime? CompletedAt { get; set; }
}
