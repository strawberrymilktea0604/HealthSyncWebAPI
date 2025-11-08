namespace HealthSync.Application.DTOs.Auth;

public record AuthResponse(string AccessToken, string RefreshToken, string UserId, string Email, string Role, string FullName, int ContributionPoints);