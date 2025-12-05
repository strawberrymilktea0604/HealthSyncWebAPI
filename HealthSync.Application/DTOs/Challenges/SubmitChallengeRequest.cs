using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HealthSync.Application.DTOs.Challenges;

public class SubmitChallengeRequest
{
    [Required(ErrorMessage = "SubmissionText is required")]
    [StringLength(1000, ErrorMessage = "SubmissionText cannot exceed 1000 characters")]
    public string SubmissionText { get; set; } = string.Empty;

    public IFormFile? SubmissionImage { get; set; }
}
