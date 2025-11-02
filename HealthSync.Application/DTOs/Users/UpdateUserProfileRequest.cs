namespace HealthSync.Application.DTOs.Users;

public class UpdateUserProfileRequest
{
    public string FullName { get; set; } = null!;
    public string? Bio { get; set; }
    public string? Gender { get; set; }
    public DateTime DateOfBirth { get; set; }
    public decimal Height { get; set; }
    public decimal Weight { get; set; }
    public string ActivityLevel { get; set; } = null!;
}