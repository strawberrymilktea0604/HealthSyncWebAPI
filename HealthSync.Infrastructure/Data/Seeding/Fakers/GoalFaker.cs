using Bogus;
using HealthSync.Domain.Entities;

namespace HealthSync.Infrastructure.Data.Seeding.Fakers;

/// <summary>
/// Bogus faker for Goal entities with ProgressRecords.
/// Generates realistic goal data for demo purposes.
/// Prioritizes InProgress and Upcoming goals.
/// </summary>
public static class GoalFaker
{
    private static readonly string?[] NoteTemplates =
    {
        "Bắt đầu hành trình mới!",
        "Tiến độ tốt, tiếp tục cố gắng.",
        "Cần tăng cường độ tập luyện.",
        "Đã giảm được một ít, tiếp tục duy trì.",
        "Tăng cân ổn định, đúng kế hoạch.",
        "Có chút thay đổi, cần theo dõi thêm.",
        "Kết quả khả quan tuần này.",
        "Cần điều chỉnh chế độ ăn uống.",
        "Đang trong quá trình hồi phục.",
        null
    };

    // Goal templates for realistic data
    private static readonly GoalTemplate[] GoalTemplates =
    {
        // Weight Loss goals
        new(GoalType.WeightLoss, "kg", 3m, 10m, 60, 120),
        new(GoalType.WeightLoss, "kg", 5m, 15m, 90, 180),
        
        // Weight Gain goals
        new(GoalType.WeightGain, "kg", 2m, 8m, 60, 120),
        new(GoalType.WeightGain, "kg", 3m, 10m, 90, 150),
        
        // Maintain Weight goals
        new(GoalType.MaintainWeight, "kg", 0m, 2m, 30, 90),
        
        // Body Measurement goals
        new(GoalType.BodyMeasurement, "cm", 2m, 5m, 30, 90),  // Waist
        new(GoalType.BodyMeasurement, "cm", 3m, 8m, 60, 120), // Chest
        new(GoalType.BodyMeasurement, "%", 2m, 5m, 60, 120)   // Body fat
    };

    /// <summary>
    /// Generates a goal with progress records for a user.
    /// Prioritizes InProgress (60%) and Upcoming (30%) statuses.
    /// </summary>
    public static Goal GenerateGoalWithProgress(int userId, DateTime referenceDate, Faker faker)
    {
        var template = faker.PickRandom(GoalTemplates);
        var status = DetermineGoalStatus(faker);
        var (startDate, endDate) = CalculateDates(status, referenceDate, template, faker);

        var goal = new Goal
        {
            UserId = userId,
            GoalType = template.Type,
            TargetValue = faker.Random.Decimal(template.MinTarget, template.MaxTarget),
            Unit = template.Unit,
            StartDate = startDate,
            EndDate = endDate,
            Status = status,
            CreatedAt = startDate.AddDays(-faker.Random.Int(0, 7)),
            UpdatedAt = DateTime.UtcNow,
            CompletedAt = status == GoalStatus.Completed ? endDate : null
        };

        // Generate progress records for active or completed goals
        if (status != GoalStatus.Cancelled && startDate <= referenceDate)
        {
            GenerateProgressRecords(goal, referenceDate, faker);
        }

        return goal;
    }

    /// <summary>
    /// Generates multiple goals for a user with varying statuses.
    /// </summary>
    public static List<Goal> GenerateGoalsForUser(int userId, int count, DateTime referenceDate, Faker faker)
    {
        var goals = new List<Goal>();
        
        for (int i = 0; i < count; i++)
        {
            var goal = GenerateGoalWithProgress(userId, referenceDate, faker);
            goals.Add(goal);
        }

        return goals;
    }

    private static GoalStatus DetermineGoalStatus(Faker faker)
    {
        var roll = faker.Random.Int(1, 100);
        
        return roll switch
        {
            <= 60 => GoalStatus.InProgress,   // 60% InProgress
            <= 90 => GoalStatus.Completed,    // 30% will become Upcoming (handled in date calculation)
            _ => GoalStatus.Cancelled          // 10% Cancelled
        };
    }

    private static (DateTime StartDate, DateTime EndDate) CalculateDates(
        GoalStatus status,
        DateTime referenceDate,
        GoalTemplate template,
        Faker faker)
    {
        var durationDays = faker.Random.Int(template.MinDurationDays, template.MaxDurationDays);

        return status switch
        {
            GoalStatus.InProgress => (
                referenceDate.AddDays(-faker.Random.Int(7, durationDays / 2)),
                referenceDate.AddDays(faker.Random.Int(durationDays / 2, durationDays))
            ),
            GoalStatus.Completed => (
                referenceDate.AddDays(-durationDays - faker.Random.Int(7, 30)),
                referenceDate.AddDays(-faker.Random.Int(1, 14))
            ),
            GoalStatus.Cancelled => (
                referenceDate.AddDays(-faker.Random.Int(30, 90)),
                referenceDate.AddDays(faker.Random.Int(30, 90))
            ),
            _ => (referenceDate, referenceDate.AddDays(durationDays))
        };
    }

    private static void GenerateProgressRecords(Goal goal, DateTime referenceDate, Faker faker)
    {
        var progressCount = CalculateProgressCount(goal, referenceDate, faker);
        if (progressCount <= 0) return;

        var effectiveEndDate = GetEffectiveEndDate(goal, referenceDate);
        var daysBetween = (effectiveEndDate - goal.StartDate).Days;
        if (daysBetween <= 0) return;

        // Calculate progress values based on goal type
        var initialValue = CalculateInitialValue(goal, faker);
        var progressPerRecord = goal.TargetValue / progressCount * faker.Random.Decimal(0.8m, 1.2m);

        AddProgressRecordsToGoal(goal, progressCount, daysBetween, effectiveEndDate, initialValue, progressPerRecord, faker);
    }

    private static DateTime GetEffectiveEndDate(Goal goal, DateTime referenceDate)
    {
        if (goal.Status == GoalStatus.Completed)
        {
            return goal.EndDate;
        }
        
        return referenceDate < goal.EndDate ? referenceDate : goal.EndDate;
    }

    private static void AddProgressRecordsToGoal(
        Goal goal,
        int progressCount,
        int daysBetween,
        DateTime effectiveEndDate,
        decimal initialValue,
        decimal progressPerRecord,
        Faker faker)
    {
        for (int i = 0; i < progressCount; i++)
        {
            var recordDate = goal.StartDate.AddDays(daysBetween / progressCount * (i + 1));
            if (recordDate > effectiveEndDate) break;

            var progressValue = CalculateProgressValue(initialValue, progressPerRecord, i, goal.GoalType, faker);
            var record = CreateProgressRecord(goal, recordDate, progressValue, faker);
            goal.ProgressRecords.Add(record);
        }
    }

    private static ProgressRecord CreateProgressRecord(Goal goal, DateTime recordDate, decimal progressValue, Faker faker)
    {
        var isWeightGoal = goal.GoalType is GoalType.WeightLoss or GoalType.WeightGain or GoalType.MaintainWeight;
        var isBodyMeasurementCm = goal.GoalType == GoalType.BodyMeasurement && goal.Unit == "cm";

        return new ProgressRecord
        {
            RecordDate = recordDate,
            RecordedValue = progressValue,
            WeightKg = isWeightGoal ? progressValue : null,
            WaistCm = isBodyMeasurementCm ? faker.Random.Decimal(60m, 100m) : null,
            ChestCm = faker.Random.Bool(0.3f) ? faker.Random.Decimal(80m, 120m) : null,
            HipCm = faker.Random.Bool(0.3f) ? faker.Random.Decimal(85m, 110m) : null,
            Notes = faker.PickRandom(NoteTemplates),
            CreatedAt = recordDate,
            UpdatedAt = recordDate
        };
    }

    private static int CalculateProgressCount(Goal goal, DateTime referenceDate, Faker faker)
    {
        var effectiveEndDate = goal.Status == GoalStatus.Completed
            ? goal.EndDate
            : (referenceDate < goal.EndDate ? referenceDate : goal.EndDate);

        var daysPassed = (effectiveEndDate - goal.StartDate).Days;
        
        // 1 record per week on average, with some randomization
        var baseCount = Math.Max(1, daysPassed / 7);
        return faker.Random.Int(Math.Max(1, baseCount - 2), baseCount + 2);
    }

    private static decimal CalculateInitialValue(Goal goal, Faker faker)
    {
        return goal.GoalType switch
        {
            GoalType.WeightLoss => faker.Random.Decimal(65m, 95m),
            GoalType.WeightGain => faker.Random.Decimal(50m, 70m),
            GoalType.MaintainWeight => faker.Random.Decimal(60m, 80m),
            GoalType.BodyMeasurement => faker.Random.Decimal(70m, 100m),
            _ => 70m
        };
    }

    private static decimal CalculateProgressValue(
        decimal initialValue,
        decimal progressPerRecord,
        int recordIndex,
        GoalType goalType,
        Faker faker)
    {
        var variance = faker.Random.Decimal(-0.5m, 0.5m);
        var direction = goalType == GoalType.WeightLoss ? -1 : 1;
        
        return Math.Round(
            initialValue + (direction * progressPerRecord * (recordIndex + 1)) + variance,
            1
        );
    }

    private sealed record GoalTemplate(
        GoalType Type,
        string Unit,
        decimal MinTarget,
        decimal MaxTarget,
        int MinDurationDays,
        int MaxDurationDays
    );
}
