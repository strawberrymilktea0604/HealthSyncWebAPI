using HealthSync.Application.DTOs.Users;
using Microsoft.AspNetCore.Http;

namespace HealthSync.Application.Interfaces;

public interface IUserProfileService
{
    Task<UserProfileDto?> GetUserProfileAsync(int userId);
    Task<UserProfileResponse?> GetUserProfileResponseAsync(int userId);
    Task<UserProfileDto> CreateUserProfileAsync(CreateUserProfileRequest request, int userId);
    Task<UserProfileDto> UpdateUserProfileAsync(UpdateUserProfileRequest request, int userId);
    Task DeleteUserProfileAsync(int userId);
    Task UpdateAvatarAsync(int userId, string avatarUrl);
    Task<UserStatsDto> GetUserStatsAsync(int userId);
}