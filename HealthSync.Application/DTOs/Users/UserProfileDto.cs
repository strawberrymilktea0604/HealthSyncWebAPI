using System.ComponentModel.DataAnnotations;

namespace HealthSync.Application.DTOs.Users;

public class UserProfileDto
{
    public int UserProfileId { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public string? ActivityLevel { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}