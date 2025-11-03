using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.Exercises;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;

namespace HealthSync.Application.Services;

public class ExerciseService : IExerciseService
{
    private readonly IExerciseRepository _exerciseRepository;

    public ExerciseService(IExerciseRepository exerciseRepository)
    {
        _exerciseRepository = exerciseRepository;
    }

    public async Task<IEnumerable<ExerciseDto>> GetAllExercisesAsync()
    {
        var exercises = await _exerciseRepository.GetAllAsync();
        return exercises.Select(e => new ExerciseDto
        {
            Id = e.ExerciseId,
            Name = e.Name,
            MuscleGroup = e.MuscleGroup,
            Difficulty = e.Difficulty,
            Equipment = e.Equipment,
            Description = e.Description,
            ImageUrl = e.ImageUrl
        });
    }

    public async Task<PaginatedResult<ExerciseDto>> GetExercisesAsync(string? muscleGroup, string? difficulty, int pageNumber, int pageSize)
    {
        // Validate pagination parameters
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 10;

        var (exercises, totalItems) = await _exerciseRepository.GetFilteredAsync(muscleGroup, difficulty, pageNumber, pageSize);

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var items = exercises.Select(e => new ExerciseDto
        {
            Id = e.ExerciseId,
            Name = e.Name,
            MuscleGroup = e.MuscleGroup,
            Difficulty = e.Difficulty,
            Equipment = e.Equipment,
            Description = e.Description,
            ImageUrl = e.ImageUrl
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
            MuscleGroup = exercise.MuscleGroup,
            Difficulty = exercise.Difficulty,
            Equipment = exercise.Equipment,
            Description = exercise.Description,
            ImageUrl = exercise.ImageUrl
        };
    }

    public async Task<ExerciseDto> CreateExerciseAsync(CreateExerciseRequest request)
    {
        if (await _exerciseRepository.ExistsByNameAsync(request.Name))
        {
            throw new InvalidOperationException("Exercise with this name already exists");
        }

        var exercise = new Exercise
        {
            Name = request.Name,
            MuscleGroup = request.MuscleGroup,
            Difficulty = request.Difficulty,
            Equipment = request.Equipment,
            Description = request.Description,
            ImageUrl = request.ImageUrl
        };

        var createdExercise = await _exerciseRepository.AddAsync(exercise);

        return new ExerciseDto
        {
            Id = createdExercise.ExerciseId,
            Name = createdExercise.Name,
            MuscleGroup = createdExercise.MuscleGroup,
            Difficulty = createdExercise.Difficulty,
            Equipment = createdExercise.Equipment,
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
            exercise.MuscleGroup = request.MuscleGroup;
        if (!string.IsNullOrWhiteSpace(request.Difficulty))
            exercise.Difficulty = request.Difficulty;
        if (request.Equipment != null)
            exercise.Equipment = request.Equipment;
        if (request.Description != null)
            exercise.Description = request.Description;
        if (request.ImageUrl != null)
            exercise.ImageUrl = request.ImageUrl;

        await _exerciseRepository.UpdateAsync(exercise);

        return new ExerciseDto
        {
            Id = exercise.ExerciseId,
            Name = exercise.Name,
            MuscleGroup = exercise.MuscleGroup,
            Difficulty = exercise.Difficulty,
            Equipment = exercise.Equipment,
            Description = exercise.Description,
            ImageUrl = exercise.ImageUrl
        };
    }

    public async Task DeleteExerciseAsync(int id)
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