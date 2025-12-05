using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthSync.Infrastructure.Repositories;

public class ChallengeParticipationRepository : IChallengeParticipationRepository
{
    private readonly ApplicationDbContext _context;

    public ChallengeParticipationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ChallengeParticipation?> GetByIdWithDetailsAsync(int participationId)
    {
        return await _context.ChallengeParticipations
            .Include(p => p.Challenge)
            .Include(p => p.User)
            .Include(p => p.ReviewedByAdmin)
            .FirstOrDefaultAsync(p => p.ParticipationId == participationId);
    }

    public async Task<ChallengeParticipation?> GetByIdAsync(int participationId)
    {
        return await _context.ChallengeParticipations
            .FirstOrDefaultAsync(p => p.ParticipationId == participationId);
    }

    public async Task<List<ChallengeParticipation>> GetByChallengeIdAsync(int challengeId)
    {
        return await _context.ChallengeParticipations
            .Where(p => p.ChallengeId == challengeId)
            .Include(p => p.User)
            .OrderByDescending(p => p.JoinedDate)
            .ToListAsync();
    }

    public async Task<List<ChallengeParticipation>> GetByChallengeAndStatusAsync(int challengeId, ParticipationStatus status)
    {
        return await _context.ChallengeParticipations
            .Where(p => p.ChallengeId == challengeId && p.Status == status)
            .Include(p => p.User)
            .OrderByDescending(p => p.SubmittedAt ?? p.JoinedDate)
            .ToListAsync();
    }

    public async Task<ChallengeParticipation?> GetUserParticipationAsync(int challengeId, int userId)
    {
        return await _context.ChallengeParticipations
            .FirstOrDefaultAsync(p => p.ChallengeId == challengeId && p.UserId == userId);
    }

    public async Task<bool> IsUserParticipatedAsync(int challengeId, int userId)
    {
        return await _context.ChallengeParticipations
            .AnyAsync(p => p.ChallengeId == challengeId && p.UserId == userId);
    }

    public async Task<int> GetParticipantCountAsync(int challengeId)
    {
        return await _context.ChallengeParticipations
            .Where(p => p.ChallengeId == challengeId)
            .CountAsync();
    }

    public async Task<int> GetPendingApprovalsCountAsync()
    {
        return await _context.ChallengeParticipations
            .Where(p => p.Status == ParticipationStatus.PendingApproval)
            .CountAsync();
    }

    public async Task<(List<ChallengeParticipation> Items, int TotalCount)> GetAllPendingApprovalsAsync(int page = 1, int pageSize = 20)
    {
        var query = _context.ChallengeParticipations
            .Where(p => p.Status == ParticipationStatus.PendingApproval)
            .Include(p => p.Challenge)
            .Include(p => p.User)
            .ThenInclude(u => u.UserProfile)
            .OrderByDescending(p => p.SubmittedAt ?? p.JoinedDate);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<ChallengeParticipation> AddAsync(ChallengeParticipation participation)
    {
        _context.ChallengeParticipations.Add(participation);
        return Task.FromResult(participation);
    }

    public Task UpdateAsync(ChallengeParticipation participation)
    {
        _context.ChallengeParticipations.Update(participation);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(int participationId)
    {
        var participation = await GetByIdAsync(participationId);
        if (participation is not null)
        {
            _context.ChallengeParticipations.Remove(participation);
        }
    }

    public async Task<IEnumerable<ChallengeParticipation>> GetAllAsync()
    {
        return await _context.ChallengeParticipations.ToListAsync();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
