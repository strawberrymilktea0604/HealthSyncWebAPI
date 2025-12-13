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
        return await _context.Leaderboards
            .Include(l => l.User)
                .ThenInclude(u => u.UserProfile)
            .FirstOrDefaultAsync(l => l.UserId == userId);
    }

    public async Task<IEnumerable<Leaderboard>> GetAllAsync()
    {
        return await _context.Leaderboards.ToListAsync();
    }

    public async Task UpdateAsync(Leaderboard leaderboard)
    {
        _context.Leaderboards.Update(leaderboard);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> SetRankTitleAsync(int userId, string? rankTitle)
    {
        var leaderboard = await _context.Leaderboards.FirstOrDefaultAsync(l => l.UserId == userId);
        
        if (leaderboard is null)
            return false;

        leaderboard.RankTitle = rankTitle;
        leaderboard.UpdatedAt = DateTime.UtcNow;

        _context.Leaderboards.Update(leaderboard);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<Leaderboard>> GetTopUsersAsync(int limit = 100)
    {
        return await _context.Leaderboards
            .Include(l => l.User)
                .ThenInclude(u => u.UserProfile)
            .OrderByDescending(l => l.TotalPoints)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> GetHigherPointsCountAsync(int points)
    {
        return await _context.Leaderboards
            .CountAsync(l => l.TotalPoints > points);
    }

    public async Task<(IEnumerable<Leaderboard> Items, int TotalCount)> GetLeaderboardAsync(int pageNumber, int pageSize)
    {
        var totalCount = await _context.Leaderboards.CountAsync();

        var items = await _context.Leaderboards
            .Include(l => l.User)
                .ThenInclude(u => u.UserProfile)
            .OrderByDescending(l => l.TotalPoints)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}