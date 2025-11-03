using HealthSync.Application.DTOs.Users;
using HealthSync.Application.Interfaces;

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
            profile.Gender,
            profile.DateOfBirth,
            profile.InitialHeightCm,
            profile.InitialWeightKg,
            profile.CurrentHeightCm,
            profile.CurrentWeightKg,
            profile.ActivityLevel,
            profile.AvatarUrl,
            profile.CreatedAt
        );
    }

    public async Task<UserProfileResponse?> GetUserProfileResponseAsync(int userId)
    {
        var profile = await _userProfileRepository.GetByUserIdAsync(userId);
        if (profile == null) return null;

        return new UserProfileResponse(
            profile.UserProfileId,
            profile.FullName,
            profile.Gender,
            profile.DateOfBirth,
            profile.InitialHeightCm,
            profile.InitialWeightKg,
            profile.CurrentHeightCm,
            profile.CurrentWeightKg,
            profile.ActivityLevel,
            profile.AvatarUrl,
            profile.CreatedAt
        );
    }

    public async Task<UserProfileDto> CreateUserProfileAsync(CreateUserProfileRequest request, int userId)
    {
        // Implementation for creating profile
        throw new NotImplementedException();
    }

    public async Task<UserProfileDto> UpdateUserProfileAsync(UpdateUserProfileRequest request, int userId)
    {
        // Implementation for updating profile
        throw new NotImplementedException();
    }

    public async Task DeleteUserProfileAsync(int userId)
    {
        // Implementation for deleting profile
        throw new NotImplementedException();
    }
}