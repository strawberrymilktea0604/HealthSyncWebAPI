using HealthSync.Application.DTOs.Leaderboard;
using HealthSync.Application.DTOs;

namespace HealthSync.Application.Interfaces;

public interface ILeaderboardService
{
    Task<IEnumerable<LeaderboardEntryDto>> GetTopUsersAsync(int limit = 100);
    Task<UserRankDto?> GetUserRankAsync(int userId);
    Task<PaginatedResult<LeaderboardEntryDto>> GetLeaderboardAsync(int pageNumber = 1, int pageSize = 20);
    Task<IEnumerable<LeaderboardUserDto>> GetTopUsersByContributionPointsAsync(int limit = 100);
}