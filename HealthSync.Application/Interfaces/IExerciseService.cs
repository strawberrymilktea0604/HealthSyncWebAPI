using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.Exercises;

namespace HealthSync.Application.Interfaces;

public interface IExerciseService
{
    Task<IEnumerable<ExerciseDto>> GetAllExercisesAsync();
    Task<PaginatedResult<ExerciseDto>> GetExercisesAsync(string? muscleGroup, string? difficulty, int pageNumber, int pageSize);
    Task<ExerciseDto?> GetExerciseByIdAsync(int id);
    Task<ExerciseDto> CreateExerciseAsync(CreateExerciseRequest request);
    Task<ExerciseDto> UpdateExerciseAsync(int id, UpdateExerciseRequest request);
    Task DeleteExerciseAsync(int id);
}