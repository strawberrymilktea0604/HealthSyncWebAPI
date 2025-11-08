using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.Exercises;
using Microsoft.AspNetCore.Http;

namespace HealthSync.Application.Interfaces;

public interface IExerciseService
{
    Task<IEnumerable<ExerciseDto>> GetAllExercisesAsync();
    Task<PaginatedResult<ExerciseDto>> GetExercisesAsync(string? muscleGroup, string? difficulty, string? equipment, int pageNumber, int pageSize);
    Task<ExerciseDto?> GetExerciseByIdAsync(int id);
    Task<ExerciseDto> CreateExerciseAsync(CreateExerciseRequest request, int adminId);
    Task<ExerciseDto> UpdateExerciseAsync(int id, UpdateExerciseRequest request);
    Task<ExerciseDto> UploadExerciseImageAsync(int id, IFormFile file);
    Task DeleteExerciseAsync(int id);
}