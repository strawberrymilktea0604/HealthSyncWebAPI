using Bogus;
using HealthSync.Domain.Entities;

namespace HealthSync.Infrastructure.Data.Seeding.Fakers;

/// <summary>
/// Bogus faker for WorkoutLog and ExerciseSession entities.
/// Generates realistic workout data for demo purposes.
/// </summary>
public sealed class WorkoutLogFaker : Faker<WorkoutLog>
{
    public WorkoutLogFaker(int userId, DateTime workoutDate)
    {
        RuleFor(w => w.UserId, userId)
            .RuleFor(w => w.WorkoutDate, workoutDate)
            .RuleFor(w => w.TotalDurationMinutes, f => f.Random.Int(30, 90))
            .RuleFor(w => w.EstimatedCaloriesBurned, 0) // Will be calculated
            .RuleFor(w => w.Notes, f => f.PickRandom(WorkoutNotes))
            .RuleFor(w => w.CreatedAt, workoutDate);
    }

    private static readonly string?[] WorkoutNotes =
    {
        "Buổi tập tốt, cảm thấy khỏe!",
        "Hơi mệt nhưng cố gắng hoàn thành.",
        "Tăng cường độ so với tuần trước.",
        "Focus vào form bài tập.",
        "Warm up kỹ, tập xong rất sảng khoái.",
        "Push day - đẩy ngực và vai.",
        "Pull day - kéo lưng và tay.",
        "Leg day - tập chân nặng.",
        "Full body workout.",
        null
    };

    public static WorkoutLog GenerateWithSessions(
        int userId,
        DateTime workoutDate,
        IReadOnlyList<Exercise> exercises,
        Faker faker)
    {
        var workoutFaker = new WorkoutLogFaker(userId, workoutDate);
        var workout = workoutFaker.Generate();

        // Generate 3-6 exercise sessions per workout
        var sessionCount = faker.Random.Int(3, 6);
        var selectedExercises = faker.PickRandom(exercises, sessionCount).ToList();

        decimal totalCalories = 0;

        for (int i = 0; i < selectedExercises.Count; i++)
        {
            var exercise = selectedExercises[i];
            var session = GenerateExerciseSession(exercise, i, faker);
            workout.ExerciseSessions.Add(session);

            // Calculate calories burned
            if (exercise.CaloriesPerMinute.HasValue && session.DurationMinutes.HasValue)
            {
                totalCalories += exercise.CaloriesPerMinute.Value * session.DurationMinutes.Value;
            }
            else if (exercise.CaloriesPerMinute.HasValue)
            {
                // Estimate duration from sets/reps (about 45 seconds per set)
                var estimatedMinutes = session.Sets * 0.75m;
                totalCalories += exercise.CaloriesPerMinute.Value * estimatedMinutes;
            }
        }

        workout.EstimatedCaloriesBurned = Math.Round(totalCalories, 1);
        return workout;
    }

    private static ExerciseSession GenerateExerciseSession(Exercise exercise, int orderIndex, Faker faker)
    {
        var isCardio = exercise.MuscleGroup == MuscleGroup.Cardio;

        return new ExerciseSession
        {
            ExerciseId = exercise.ExerciseId,
            Sets = isCardio ? 1 : faker.Random.Int(3, 5),
            Reps = isCardio ? 1 : faker.Random.Int(8, 15),
            WeightKg = isCardio ? null : faker.Random.Decimal(10, 80),
            RestSeconds = isCardio ? null : faker.Random.Int(60, 120),
            Rpe = faker.Random.Bool(0.7f) ? faker.Random.Int(6, 9) : null,
            DurationMinutes = isCardio ? faker.Random.Int(15, 30) : null,
            OrderIndex = orderIndex,
            Notes = faker.Random.Bool(0.1f) ? faker.Lorem.Sentence() : null
        };
    }
}
