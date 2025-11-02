namespace HealthSync.Application.DTOs.Users;

public class UserProfileResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string? Bio { get; set; }
    public string? Gender { get; set; }
    public DateTime DateOfBirth { get; set; }
    public int Age => DateTime.Today.Year - DateOfBirth.Year - 
        (DateTime.Today.DayOfYear < DateOfBirth.DayOfYear ? 1 : 0);
    public decimal Height { get; set; }
    public decimal Weight { get; set; }
    public string ActivityLevel { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}