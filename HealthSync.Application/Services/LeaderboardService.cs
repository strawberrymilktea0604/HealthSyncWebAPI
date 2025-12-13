using HealthSync.Application.DTOs.Leaderboard;
using HealthSync.Application.DTOs;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;

namespace HealthSync.Application.Services;

public class LeaderboardService : ILeaderboardService
{
    private readonly ILeaderboardRepository _leaderboardRepository;
    private readonly IUserProfileRepository _userProfileRepository;

    public LeaderboardService(ILeaderboardRepository leaderboardRepository, IUserProfileRepository userProfileRepository)
    {
        _leaderboardRepository = leaderboardRepository;
        _userProfileRepository = userProfileRepository;
    }

    public async Task<IEnumerable<LeaderboardEntryDto>> GetTopUsersAsync(int limit = 100)
    {
        var leaderboards = await _leaderboardRepository.GetTopUsersAsync(limit);

        return leaderboards.Select(l => new LeaderboardEntryDto
        {
            LeaderboardId = l.LeaderboardId,
            UserId = l.UserId,
            UserName = l.User?.UserProfile?.FullName ?? l.User?.Email ?? "Unknown",
            AvatarUrl = l.User?.UserProfile?.AvatarUrl,
            TotalPoints = l.TotalPoints,
            RankTitle = l.RankTitle,
            RankPosition = l.RankPosition,
            UpdatedAt = l.UpdatedAt
        });
    }

    public async Task<UserRankDto?> GetUserRankAsync(int userId)
    {
        var leaderboard = await _leaderboardRepository.GetByUserIdAsync(userId);
        if (leaderboard == null)
        {
            return null;
        }

        // Calculate rank position by counting users with higher points
        var higherPointsCount = await _leaderboardRepository.GetHigherPointsCountAsync(leaderboard.TotalPoints);

        return new UserRankDto
        {
            UserId = leaderboard.UserId,
            UserName = leaderboard.User?.UserProfile?.FullName ?? leaderboard.User?.Email ?? "Unknown",
            AvatarUrl = leaderboard.User?.UserProfile?.AvatarUrl,
            TotalPoints = leaderboard.TotalPoints,
            RankPosition = higherPointsCount + 1, // Rank starts from 1
            RankTitle = leaderboard.RankTitle,
            UpdatedAt = leaderboard.UpdatedAt
        };
    }

    public async Task<PaginatedResult<LeaderboardEntryDto>> GetLeaderboardAsync(int pageNumber = 1, int pageSize = 20)
    {
        var (items, totalCount) = await _leaderboardRepository.GetLeaderboardAsync(pageNumber, pageSize);

        var dtos = items.Select(l => new LeaderboardEntryDto
        {
            LeaderboardId = l.LeaderboardId,
            UserId = l.UserId,
            UserName = l.User?.UserProfile?.FullName ?? l.User?.Email ?? "Unknown",
            AvatarUrl = l.User?.UserProfile?.AvatarUrl,
            TotalPoints = l.TotalPoints,
            RankTitle = l.RankTitle,
            RankPosition = l.RankPosition,
            UpdatedAt = l.UpdatedAt
        }).ToList();

        return new PaginatedResult<LeaderboardEntryDto>(dtos, totalCount, pageNumber, pageSize);
    }

    public async Task<IEnumerable<LeaderboardUserDto>> GetTopUsersByContributionPointsAsync(int limit = 100)
    {
        var userProfiles = await _userProfileRepository.GetTopUsersByContributionPointsAsync(limit);

        return userProfiles.Select(up => new LeaderboardUserDto
        {
            UserId = up.UserId,
            FullName = up.FullName,
            AvatarUrl = up.AvatarUrl,
            ContributionPoints = up.ContributionPoints
        });
    }
}