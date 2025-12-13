using HealthSync.Domain.Entities;

namespace HealthSync.Application.Interfaces;

public interface ILeaderboardRepository
{
    Task AddAsync(Leaderboard leaderboard);
    Task<Leaderboard?> GetByUserIdAsync(int userId);
    Task<IEnumerable<Leaderboard>> GetAllAsync();
    Task<IEnumerable<Leaderboard>> GetTopUsersAsync(int limit = 100);
    Task<int> GetHigherPointsCountAsync(int points);
    Task<(IEnumerable<Leaderboard> Items, int TotalCount)> GetLeaderboardAsync(int pageNumber, int pageSize);
    Task UpdateAsync(Leaderboard leaderboard);
    Task<bool> SetRankTitleAsync(int userId, string? rankTitle);
    Task SaveChangesAsync();
}