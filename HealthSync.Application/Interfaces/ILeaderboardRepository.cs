using HealthSync.Domain.Entities;

namespace HealthSync.Application.Interfaces;

public interface ILeaderboardRepository
{
    Task AddAsync(Leaderboard leaderboard);
    Task<Leaderboard?> GetByUserIdAsync(int userId);
    Task<IEnumerable<Leaderboard>> GetAllAsync();
    Task UpdateAsync(Leaderboard leaderboard);
    Task<bool> SetRankTitleAsync(int userId, string? rankTitle);
    Task SaveChangesAsync();
}