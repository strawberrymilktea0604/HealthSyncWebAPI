using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthSync.Infrastructure.Repositories;

public class LeaderboardRepository : ILeaderboardRepository
{
    private readonly ApplicationDbContext _context;

    public LeaderboardRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Leaderboard leaderboard)
    {
        await _context.Leaderboards.AddAsync(leaderboard);
        await _context.SaveChangesAsync();
    }

    public async Task<Leaderboard?> GetByUserIdAsync(int userId)
    {
        return await _context.Leaderboards.FirstOrDefaultAsync(l => l.UserId == userId);
    }

    public async Task UpdateAsync(Leaderboard leaderboard)
    {
        _context.Leaderboards.Update(leaderboard);
        await _context.SaveChangesAsync();
    }
}