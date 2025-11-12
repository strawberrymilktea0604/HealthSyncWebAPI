using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthSync.Infrastructure.Repositories;

public class GoalRepository : IGoalRepository
{
    private readonly ApplicationDbContext _context;

    public GoalRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Goal?> GetByIdAsync(int id)
    {
        return await _context.Goals.FindAsync(id);
    }

    public async Task<IEnumerable<Goal>> GetUserGoalsAsync(int userId)
    {
        return await _context.Goals
            .Where(g => g.UserId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Goal goal)
    {
        await _context.Goals.AddAsync(goal);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Goal goal)
    {
        _context.Goals.Update(goal);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var goal = await GetByIdAsync(id);
        if (goal != null)
        {
            _context.Goals.Remove(goal);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<ProgressRecord?> GetProgressRecordByIdAsync(int id)
    {
        return await _context.ProgressRecords.FindAsync(id);
    }

    public async Task<IEnumerable<ProgressRecord>> GetProgressRecordsByGoalIdAsync(int goalId)
    {
        return await _context.ProgressRecords
            .Where(pr => pr.GoalId == goalId)
            .OrderBy(pr => pr.RecordDate)
            .ToListAsync();
    }

    public async Task AddProgressRecordAsync(ProgressRecord record)
    {
        await _context.ProgressRecords.AddAsync(record);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateProgressRecordAsync(ProgressRecord record)
    {
        _context.ProgressRecords.Update(record);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteProgressRecordAsync(int id)
    {
        var record = await GetProgressRecordByIdAsync(id);
        if (record != null)
        {
            _context.ProgressRecords.Remove(record);
            await _context.SaveChangesAsync();
        }
    }
}