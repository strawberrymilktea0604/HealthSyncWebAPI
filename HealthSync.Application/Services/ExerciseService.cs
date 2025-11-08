using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.Exercises;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace HealthSync.Application.Services;

public class ExerciseService : IExerciseService
{
    private readonly IExerciseRepository _exerciseRepository;
    private readonly IFileStorageService _fileStorageService;

    public ExerciseService(IExerciseRepository exerciseRepository, IFileStorageService fileStorageService)
    {
        _exerciseRepository = exerciseRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<IEnumerable<ExerciseDto>> GetAllExercisesAsync()
    {
        var exercises = await _exerciseRepository.GetAllAsync();
        return exercises.Select(e => new ExerciseDto
        {
            Id = e.ExerciseId,
            Name = e.Name,
            MuscleGroup = e.MuscleGroup.ToString(),
            Difficulty = e.DifficultyLevel.ToString(),
            Equipment = e.Equipment?.ToString(),
            Description = e.Description,
            Instructions = e.Instructions,
            ImageUrl = e.ImageUrl,
            VideoUrl = e.VideoUrl,
            CaloriesPerMinute = e.CaloriesPerMinute,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        });
    }

    public async Task<PaginatedResult<ExerciseDto>> GetExercisesAsync(string? muscleGroup, string? difficulty, string? equipment, int pageNumber, int pageSize)
    {
        // Validate pagination parameters
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 10;

        var (exercises, totalItems) = await _exerciseRepository.GetFilteredAsync(muscleGroup, difficulty, equipment, pageNumber, pageSize);

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var items = exercises.Select(e => new ExerciseDto
        {
            Id = e.ExerciseId,
            Name = e.Name,
            MuscleGroup = e.MuscleGroup.ToString(),
            Difficulty = e.DifficultyLevel.ToString(),
            Equipment = e.Equipment?.ToString(),
            Description = e.Description,
            Instructions = e.Instructions,
            ImageUrl = e.ImageUrl,
            VideoUrl = e.VideoUrl,
            CaloriesPerMinute = e.CaloriesPerMinute,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt
        });

        return new PaginatedResult<ExerciseDto>
        {
            Items = items,
            CurrentPage = pageNumber,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }

    public async Task<ExerciseDto?> GetExerciseByIdAsync(int id)
    {
        var exercise = await _exerciseRepository.GetByIdAsync(id);
        if (exercise == null) return null;

        return new ExerciseDto
        {
            Id = exercise.ExerciseId,
            Name = exercise.Name,
            MuscleGroup = exercise.MuscleGroup.ToString(),
            Difficulty = exercise.DifficultyLevel.ToString(),
            Equipment = exercise.Equipment?.ToString(),
            Description = exercise.Description,
            Instructions = exercise.Instructions,
            ImageUrl = exercise.ImageUrl,
            VideoUrl = exercise.VideoUrl,
            CaloriesPerMinute = exercise.CaloriesPerMinute,
            CreatedAt = exercise.CreatedAt,
            UpdatedAt = exercise.UpdatedAt
        };
    }

    public async Task<ExerciseDto> CreateExerciseAsync(CreateExerciseRequest request, int adminId)
    {
        if (await _exerciseRepository.ExistsByNameAsync(request.Name))
        {
            throw new InvalidOperationException("Exercise with this name already exists");
        }

        var exercise = new Exercise
        {
            Name = request.Name,
            MuscleGroup = Enum.Parse<MuscleGroup>(request.MuscleGroup),
            DifficultyLevel = Enum.Parse<DifficultyLevel>(request.Difficulty),
            Equipment = string.IsNullOrEmpty(request.Equipment) ? null : Enum.Parse<Equipment>(request.Equipment),
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            CreatedByAdminId = adminId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdExercise = await _exerciseRepository.AddAsync(exercise);

        return new ExerciseDto
        {
            Id = createdExercise.ExerciseId,
            Name = createdExercise.Name,
            MuscleGroup = createdExercise.MuscleGroup.ToString(),
            Difficulty = createdExercise.DifficultyLevel.ToString(),
            Equipment = createdExercise.Equipment?.ToString(),
            Description = createdExercise.Description,
            ImageUrl = createdExercise.ImageUrl
        };
    }

    public async Task<ExerciseDto> UpdateExerciseAsync(int id, UpdateExerciseRequest request)
    {
        var exercise = await _exerciseRepository.GetByIdAsync(id);
        if (exercise == null)
        {
            throw new KeyNotFoundException($"Exercise with ID {id} not found");
        }

        if (!string.IsNullOrWhiteSpace(request.Name) && await _exerciseRepository.ExistsByNameAsync(request.Name, id))
        {
            throw new InvalidOperationException("Another exercise with this name already exists");
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
            exercise.Name = request.Name;
        if (!string.IsNullOrWhiteSpace(request.MuscleGroup))
            exercise.MuscleGroup = Enum.Parse<MuscleGroup>(request.MuscleGroup);
        if (!string.IsNullOrWhiteSpace(request.Difficulty))
            exercise.DifficultyLevel = Enum.Parse<DifficultyLevel>(request.Difficulty);
        if (request.Equipment != null)
            exercise.Equipment = string.IsNullOrEmpty(request.Equipment) ? null : Enum.Parse<Equipment>(request.Equipment);
        if (request.Description != null)
            exercise.Description = request.Description;
        if (request.ImageUrl != null)
            exercise.ImageUrl = request.ImageUrl;
        exercise.UpdatedAt = DateTime.UtcNow;

        await _exerciseRepository.UpdateAsync(exercise);

        return new ExerciseDto
        {
            Id = exercise.ExerciseId,
            Name = exercise.Name,
            MuscleGroup = exercise.MuscleGroup.ToString(),
            Difficulty = exercise.DifficultyLevel.ToString(),
            Equipment = exercise.Equipment?.ToString(),
            Description = exercise.Description,
            ImageUrl = exercise.ImageUrl
        };
    }

    public async Task<ExerciseDto> UploadExerciseImageAsync(int id, IFormFile file)
    {
        var exercise = await _exerciseRepository.GetByIdAsync(id);
        if (exercise == null)
        {
            throw new KeyNotFoundException($"Exercise with ID {id} not found");
        }

        // Upload file to storage
        var imageUrl = await _fileStorageService.UploadFileAsync(file, "exercises", $"exercise_{id}_{Guid.NewGuid()}");
        
        exercise.ImageUrl = imageUrl;
        exercise.UpdatedAt = DateTime.UtcNow;

        await _exerciseRepository.UpdateAsync(exercise);

        return new ExerciseDto
        {
            Id = exercise.ExerciseId,
            Name = exercise.Name,
            MuscleGroup = exercise.MuscleGroup.ToString(),
            Difficulty = exercise.DifficultyLevel.ToString(),
            Equipment = exercise.Equipment?.ToString(),
            Description = exercise.Description,
            Instructions = exercise.Instructions,
            ImageUrl = exercise.ImageUrl,
            VideoUrl = exercise.VideoUrl,
            CaloriesPerMinute = exercise.CaloriesPerMinute,
            CreatedAt = exercise.CreatedAt,
            UpdatedAt = exercise.UpdatedAt
        };
    }    public async Task DeleteExerciseAsync(int id)
    {
        var exercise = await _exerciseRepository.GetByIdAsync(id);
        if (exercise == null)
        {
            throw new KeyNotFoundException($"Exercise with ID {id} not found");
        }

        if (await _exerciseRepository.IsUsedInSessionsAsync(id))
        {
            throw new InvalidOperationException("Cannot delete exercise that is referenced by exercise sessions");
        }

        await _exerciseRepository.DeleteAsync(id);
    }
}
