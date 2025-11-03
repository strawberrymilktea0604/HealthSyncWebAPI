using HealthSync.Application.DTOs.Users;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace HealthSync.Application.Features.Users.Services;

public class UserProfileService : IUserProfileService
{
    private readonly IUserProfileRepository _userProfileRepository;

    public UserProfileService(IUserProfileRepository userProfileRepository)
    {
        _userProfileRepository = userProfileRepository;
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(int userId)
    {
        var profile = await _userProfileRepository.GetByUserIdAsync(userId);
        if (profile == null) return null;

        return new UserProfileDto(
            profile.UserProfileId,
            profile.UserId,
            profile.FullName,
            profile.Gender?.ToString(),
            profile.DateOfBirth,
            profile.HeightCm,
            profile.CurrentWeightKg,
            profile.ActivityLevel?.ToString(),
            profile.AvatarUrl,
            profile.ContributionPoints,
            profile.CreatedAt,
            profile.UpdatedAt
        );
    }

    public async Task<UserProfileResponse?> GetUserProfileResponseAsync(int userId)
    {
        var profile = await _userProfileRepository.GetByUserIdAsync(userId);
        if (profile == null) return null;

        return new UserProfileResponse(
            profile.UserProfileId,
            profile.FullName,
            profile.Gender?.ToString(),
            profile.DateOfBirth,
            profile.HeightCm,
            profile.CurrentWeightKg,
            profile.ActivityLevel?.ToString(),
            profile.AvatarUrl,
            profile.ContributionPoints,
            profile.CreatedAt,
            profile.UpdatedAt
        );
    }

    public async Task<UserProfileDto> CreateUserProfileAsync(CreateUserProfileRequest request, int userId)
    {
        // Implementation for creating profile
        throw new NotImplementedException();
    }

    public async Task<UserProfileDto> UpdateUserProfileAsync(UpdateUserProfileRequest request, int userId)
    {
        var existingProfile = await _userProfileRepository.GetByUserIdAsync(userId);
        if (existingProfile == null)
        {
            throw new KeyNotFoundException("User profile not found");
        }

        // Update fields
        existingProfile.FullName = request.FullName;
        if (request.Gender != null && Enum.TryParse<Gender>(request.Gender, out var gender))
        {
            existingProfile.Gender = gender;
        }
        existingProfile.DateOfBirth = request.DateOfBirth;
        existingProfile.HeightCm = request.HeightCm;
        existingProfile.CurrentWeightKg = request.CurrentWeightKg;
        if (request.ActivityLevel != null && Enum.TryParse<ActivityLevel>(request.ActivityLevel, out var activityLevel))
        {
            existingProfile.ActivityLevel = activityLevel;
        }
        existingProfile.AvatarUrl = request.AvatarUrl;
        existingProfile.UpdatedAt = DateTime.UtcNow;

        await _userProfileRepository.UpdateAsync(existingProfile);

        // Return updated DTO
        return new UserProfileDto(
            existingProfile.UserProfileId,
            existingProfile.UserId,
            existingProfile.FullName,
            existingProfile.Gender?.ToString(),
            existingProfile.DateOfBirth,
            existingProfile.HeightCm,
            existingProfile.CurrentWeightKg,
            existingProfile.ActivityLevel?.ToString(),
            existingProfile.AvatarUrl,
            existingProfile.ContributionPoints,
            existingProfile.CreatedAt,
            existingProfile.UpdatedAt
        );
    }

    public async Task DeleteUserProfileAsync(int userId)
    {
        // Implementation for deleting profile
        throw new NotImplementedException();
    }

    public async Task<string> UpdateAvatarAsync(int userId, IFormFile file)
    {
        // Implementation for updating avatar
        throw new NotImplementedException();
    }

    public async Task<UserStatsDto> GetUserStatsAsync(int userId)
    {
        // Implementation for getting user stats
        throw new NotImplementedException();
    }
}