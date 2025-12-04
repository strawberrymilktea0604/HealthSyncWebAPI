using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthSync.Infrastructure.Repositories;

public class ChallengeRepository : IChallengeRepository
{
    private readonly ApplicationDbContext _context;

    public ChallengeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Challenge?> GetByIdWithParticipationsAsync(int challengeId)
    {
        return await _context.Challenges
            .Include(c => c.CreatedByAdmin)
            .Include(c => c.Participations)
            .FirstOrDefaultAsync(c => c.ChallengeId == challengeId);
    }

    public async Task<Challenge?> GetByIdAsync(int challengeId)
    {
        return await _context.Challenges
            .Include(c => c.CreatedByAdmin)
            .FirstOrDefaultAsync(c => c.ChallengeId == challengeId);
    }

    public async Task<(List<Challenge> Items, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 20)
    {
        var query = _context.Challenges
            .Include(c => c.CreatedByAdmin)
            .OrderByDescending(c => c.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<List<Challenge>> GetByStatusAsync(ChallengeStatus status)
    {
        return await _context.Challenges
            .Where(c => c.Status == status)
            .Include(c => c.CreatedByAdmin)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public Task<Challenge> AddAsync(Challenge challenge)
    {
        _context.Challenges.Add(challenge);
        return Task.FromResult(challenge);
    }

    public Task UpdateAsync(Challenge challenge)
    {
        challenge.UpdatedAt = DateTime.UtcNow;
        _context.Challenges.Update(challenge);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(int challengeId)
    {
        var challenge = await GetByIdAsync(challengeId);
        if (challenge is not null)
        {
            _context.Challenges.Remove(challenge);
        }
    }

    public async Task<bool> ExistsAsync(int challengeId)
    {
        return await _context.Challenges
            .AnyAsync(c => c.ChallengeId == challengeId);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
