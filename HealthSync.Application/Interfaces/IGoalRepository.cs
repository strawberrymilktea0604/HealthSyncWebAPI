using HealthSync.Domain.Entities;

namespace HealthSync.Application.Interfaces;

public interface IGoalRepository
{
    Task<Goal?> GetByIdAsync(int id);
    Task<IEnumerable<Goal>> GetUserGoalsAsync(int userId);
    Task AddAsync(Goal goal);
    Task UpdateAsync(Goal goal);
    Task DeleteAsync(int id);
    Task<ProgressRecord?> GetProgressRecordByIdAsync(int id);
    Task<IEnumerable<ProgressRecord>> GetProgressRecordsByGoalIdAsync(int goalId);
    Task AddProgressRecordAsync(ProgressRecord record);
    Task UpdateProgressRecordAsync(ProgressRecord record);
    Task DeleteProgressRecordAsync(int id);
}