namespace HealthSync.Application.DTOs.Users;

public record AdminUserDto
(
    int Id,
    string Email,
    string Role,
    bool IsActive,
    string FullName,
    DateTime CreatedAt
);
