using HealthSync.Application.DTOs.Users;
using HealthSync.Application.Interfaces;

namespace HealthSync.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILeaderboardRepository _leaderboardRepository;
    private readonly IUserProfileRepository _userProfileRepository;

    public UserService(
        IUserRepository userRepository,
        ILeaderboardRepository leaderboardRepository,
        IUserProfileRepository userProfileRepository)
    {
        _userRepository = userRepository;
        _leaderboardRepository = leaderboardRepository;
        _userProfileRepository = userProfileRepository;
    }

    public async Task UpdateUserStatusAsync(int userId, bool isActive)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }
        user.IsActive = isActive;
        await _userRepository.UpdateAsync(user);
    }

    public async Task UpdateUserRoleAsync(int userId, string role)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }
        user.Role = role;
        await _userRepository.UpdateAsync(user);
    }

    public async Task<UserRankTitleDto?> SetUserRankTitleAsync(int userId, string? rankTitle)
    {
        // Check if user exists
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            return null;

        // Update leaderboard with new rank title
        var success = await _leaderboardRepository.SetRankTitleAsync(userId, rankTitle);
        if (!success)
            return null;

        // Get updated leaderboard entry
        var leaderboard = await _leaderboardRepository.GetByUserIdAsync(userId);
        if (leaderboard is null)
            return null;

        // Get user profile for full name
        var userProfile = await _userProfileRepository.GetByUserIdAsync(userId);

        return new UserRankTitleDto
        {
            UserId = userId,
            FullName = userProfile?.FullName,
            RankTitle = leaderboard.RankTitle,
            TotalPoints = leaderboard.TotalPoints,
            RankPosition = leaderboard.RankPosition,
            UpdatedAt = leaderboard.UpdatedAt
        };
    }
}