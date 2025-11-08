using HealthSync.Domain.Entities;

namespace HealthSync.Application.Interfaces;

public interface IExerciseRepository
{
    Task<Exercise?> GetByIdAsync(int id);
    Task<IEnumerable<Exercise>> GetAllAsync();
    Task<(IEnumerable<Exercise>, int)> GetFilteredAsync(string? muscleGroup, string? difficulty, string? equipment, int pageNumber, int pageSize);
    Task<Exercise> AddAsync(Exercise exercise);
    Task UpdateAsync(Exercise exercise);
    Task DeleteAsync(int id);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    Task<bool> IsUsedInSessionsAsync(int id);
}