using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthSync.Infrastructure.Repositories;

public class ExerciseRepository : IExerciseRepository
{
    private readonly ApplicationDbContext _context;

    public ExerciseRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Exercise?> GetByIdAsync(int id)
    {
        return await _context.Exercises.FindAsync(id);
    }

    public async Task<IEnumerable<Exercise>> GetAllAsync()
    {
        return await _context.Exercises.OrderBy(e => e.Name).ToListAsync();
    }

    public async Task<(IEnumerable<Exercise>, int)> GetFilteredAsync(string? muscleGroup, string? difficulty, string? equipment, int pageNumber, int pageSize)
    {
        var query = _context.Exercises.AsQueryable();

        // Apply filters if provided
        if (!string.IsNullOrWhiteSpace(muscleGroup))
            query = query.Where(e => e.MuscleGroup.ToString().ToLower() == muscleGroup.ToLower());
        if (!string.IsNullOrWhiteSpace(difficulty))
            query = query.Where(e => e.DifficultyLevel.ToString().ToLower() == difficulty.ToLower());
        if (!string.IsNullOrWhiteSpace(equipment))
            query = query.Where(e => e.Equipment.HasValue && e.Equipment.Value.ToString().ToLower() == equipment.ToLower());

        // Get total count
        var totalItems = await query.CountAsync();

        // Apply pagination and ordering
        var exercises = await query
            .OrderBy(e => e.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (exercises, totalItems);
    }

    public async Task<Exercise> AddAsync(Exercise exercise)
    {
        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync();
        return exercise;
    }

    public async Task UpdateAsync(Exercise exercise)
    {
        _context.Exercises.Update(exercise);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var exercise = await GetByIdAsync(id);
        if (exercise != null)
        {
            _context.Exercises.Remove(exercise);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
    {
        var query = _context.Exercises.Where(e => e.Name.ToLower() == name.ToLower());
        if (excludeId.HasValue)
        {
            query = query.Where(e => e.ExerciseId != excludeId.Value);
        }
        return await query.AnyAsync();
    }

    public async Task<bool> IsUsedInSessionsAsync(int id)
    {
        return await _context.ExerciseSessions.AnyAsync(es => es.ExerciseId == id);
    }
}