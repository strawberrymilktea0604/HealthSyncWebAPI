using HealthSync.Domain.Entities;

namespace HealthSync.Application.Interfaces;

public interface ILeaderboardRepository
{
    Task AddAsync(Leaderboard leaderboard);
    Task<Leaderboard?> GetByUserIdAsync(int userId);
    Task UpdateAsync(Leaderboard leaderboard);
}