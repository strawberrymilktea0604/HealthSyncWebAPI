using HealthSync.Application.DTOs.Users;

namespace HealthSync.Application.Interfaces;

public interface IUserProfileService
{
    Task<UserProfileDto?> GetUserProfileAsync(int userId);
    Task<UserProfileDto> CreateUserProfileAsync(CreateUserProfileRequest request, int userId);
    Task<UserProfileDto> UpdateUserProfileAsync(UpdateUserProfileRequest request, int userId);
    Task DeleteUserProfileAsync(int userId);
}