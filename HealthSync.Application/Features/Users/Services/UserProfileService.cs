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

        return new UserProfileDto
        {
            UserProfileId = profile.UserProfileId,
            UserId = profile.UserId,
            FullName = profile.FullName,
            Gender = profile.Gender,
            DateOfBirth = profile.DateOfBirth,
            HeightCm = profile.CurrentHeightCm ?? profile.InitialHeightCm,
            WeightKg = profile.CurrentWeightKg ?? profile.InitialWeightKg,
            ActivityLevel = profile.ActivityLevel,
            AvatarUrl = profile.AvatarUrl,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = null // Entity doesn't have UpdatedAt
        };
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