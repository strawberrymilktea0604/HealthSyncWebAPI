using HealthSync.Application.DTOs.Goals;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using FluentValidation;

namespace HealthSync.Application.Services;

public class GoalService : IGoalService
{
    private readonly IGoalRepository _goalRepository;
    private readonly IUserProfileRepository _userProfileRepository;

    public GoalService(IGoalRepository goalRepository, IUserProfileRepository userProfileRepository)
    {
        _goalRepository = goalRepository;
        _userProfileRepository = userProfileRepository;
    }

    public async Task<GoalDto> CreateGoalAsync(CreateGoalRequest request, int userId)
    {
        UserProfile? userProfile = null;

        // Validate business rules
        if (request.EndDate <= request.StartDate)
            throw new ValidationException("End date must be after start date");

        if (request.StartDate < DateTime.UtcNow.Date)
            throw new ValidationException("Start date cannot be in the past");

        // Validate target value based on goal type
        if (request.GoalType == GoalType.WeightLoss || request.GoalType == GoalType.WeightGain)
        {
            userProfile = await _userProfileRepository.GetByUserIdAsync(userId);
            if (userProfile == null)
                throw new ValidationException("User profile not found. Please complete your profile first.");

            var currentWeight = userProfile.CurrentWeightKg;
            if (!currentWeight.HasValue)
                throw new ValidationException("Current weight not set in profile");

            var maxChange = currentWeight.Value * 0.3m; // Max 30% change

            if (request.GoalType == GoalType.WeightLoss && request.TargetValue >= currentWeight.Value)
                throw new ValidationException("Target weight must be less than current weight for weight loss");

            if (request.GoalType == GoalType.WeightGain && request.TargetValue <= currentWeight.Value)
                throw new ValidationException("Target weight must be greater than current weight for weight gain");

            var weightDifference = Math.Abs(request.TargetValue - currentWeight.Value);
            if (weightDifference > maxChange)
                throw new ValidationException("Target weight change cannot exceed 30% of current weight");
        }

        // Create goal
        var goal = new Goal
        {
            UserId = userId,
            GoalType = request.GoalType,
            TargetValue = request.TargetValue,
            Unit = request.Unit,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = GoalStatus.InProgress,
            CreatedAt = DateTime.UtcNow
        };

        await _goalRepository.AddAsync(goal);

        // Create initial progress record
        var initialRecord = new ProgressRecord
        {
            GoalId = goal.GoalId,
            RecordDate = request.StartDate,
            RecordedValue = GetInitialValue(request.GoalType, userProfile),
            WeightKg = userProfile?.CurrentWeightKg,
            CreatedAt = DateTime.UtcNow
        };

        await _goalRepository.AddProgressRecordAsync(initialRecord);

        return MapToDto(goal);
    }

    public async Task<GoalDto> GetGoalByIdAsync(int goalId, int userId)
    {
        var goal = await _goalRepository.GetByIdAsync(goalId);
        if (goal == null || goal.UserId != userId)
            throw new ValidationException("Goal not found");

        return MapToDto(goal);
    }

    public async Task<IEnumerable<GoalDto>> GetUserGoalsAsync(int userId)
    {
        var goals = await _goalRepository.GetUserGoalsAsync(userId);
        return goals.Select(MapToDto);
    }

    public async Task<GoalDto> UpdateGoalAsync(int goalId, UpdateGoalRequest request, int userId)
    {
        var goal = await _goalRepository.GetByIdAsync(goalId);
        if (goal == null || goal.UserId != userId)
            throw new ValidationException("Goal not found");

        if (goal.Status != GoalStatus.InProgress)
            throw new ValidationException("Cannot update completed or cancelled goals");

        // Validate business rules
        if (request.EndDate <= request.StartDate)
            throw new ValidationException("End date must be after start date");

        if (request.StartDate < DateTime.UtcNow.Date)
            throw new ValidationException("Start date cannot be in the past");

        // Update fields
        goal.GoalType = request.GoalType;
        goal.TargetValue = request.TargetValue;
        goal.Unit = request.Unit;
        goal.StartDate = request.StartDate;
        goal.EndDate = request.EndDate;
        goal.UpdatedAt = DateTime.UtcNow;

        await _goalRepository.UpdateAsync(goal);

        return MapToDto(goal);
    }

    public async Task DeleteGoalAsync(int goalId, int userId)
    {
        var goal = await _goalRepository.GetByIdAsync(goalId);
        if (goal == null || goal.UserId != userId)
            throw new ValidationException("Goal not found");

        // Delete associated progress records first
        var progressRecords = await _goalRepository.GetProgressRecordsByGoalIdAsync(goalId);
        foreach (var record in progressRecords)
        {
            await _goalRepository.DeleteProgressRecordAsync(record.ProgressRecordId);
        }

        await _goalRepository.DeleteAsync(goalId);
    }

    public async Task<ProgressRecordDto> RecordProgressAsync(RecordProgressRequest request, int userId)
    {
        var goal = await _goalRepository.GetByIdAsync(request.GoalId);
        if (goal == null || goal.UserId != userId)
            throw new ValidationException("Goal not found");

        if (goal.Status != GoalStatus.InProgress)
            throw new ValidationException("Cannot record progress for completed or cancelled goals");

        if (request.RecordDate < goal.StartDate || request.RecordDate > goal.EndDate)
            throw new ValidationException("Record date must be within goal period");

        // Check for duplicate record date
        var existingRecords = await _goalRepository.GetProgressRecordsByGoalIdAsync(request.GoalId);
        if (existingRecords.Any(r => r.RecordDate.Date == request.RecordDate.Date))
            throw new ValidationException("Progress already recorded for this date");

        var record = new ProgressRecord
        {
            GoalId = request.GoalId,
            RecordDate = request.RecordDate,
            RecordedValue = request.RecordedValue,
            WeightKg = request.WeightKg,
            WaistCm = request.WaistCm,
            ChestCm = request.ChestCm,
            HipCm = request.HipCm,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        await _goalRepository.AddProgressRecordAsync(record);

        // Check if goal is completed
        if (IsGoalCompleted(goal, request.RecordedValue))
        {
            goal.Status = GoalStatus.Completed;
            goal.CompletedAt = request.RecordDate;
            await _goalRepository.UpdateAsync(goal);
        }

        return MapToProgressDto(record);
    }

    public async Task<ProgressRecordDto> UpdateProgressRecordAsync(int recordId, UpdateProgressRequest request, int userId)
    {
        var record = await _goalRepository.GetProgressRecordByIdAsync(recordId);
        if (record == null)
            throw new ValidationException("Progress record not found");

        var goal = await _goalRepository.GetByIdAsync(record.GoalId);
        if (goal == null || goal.UserId != userId)
            throw new ValidationException("Unauthorized access to progress record");

        // Update fields
        record.RecordedValue = request.RecordedValue ?? record.RecordedValue;
        record.WeightKg = request.WeightKg ?? record.WeightKg;
        record.WaistCm = request.WaistCm ?? record.WaistCm;
        record.ChestCm = request.ChestCm ?? record.ChestCm;
        record.HipCm = request.HipCm ?? record.HipCm;
        record.Notes = request.Notes ?? record.Notes;
        record.UpdatedAt = DateTime.UtcNow;

        await _goalRepository.UpdateProgressRecordAsync(record);

        // Recheck goal completion
        if (IsGoalCompleted(goal, record.RecordedValue))
        {
            goal.Status = GoalStatus.Completed;
            goal.CompletedAt = record.RecordDate;
            await _goalRepository.UpdateAsync(goal);
        }

        return MapToProgressDto(record);
    }

    public async Task DeleteProgressRecordAsync(int recordId, int userId)
    {
        var record = await _goalRepository.GetProgressRecordByIdAsync(recordId);
        if (record == null)
            throw new ValidationException("Progress record not found");

        var goal = await _goalRepository.GetByIdAsync(record.GoalId);
        if (goal == null || goal.UserId != userId)
            throw new ValidationException("Unauthorized access to progress record");

        await _goalRepository.DeleteProgressRecordAsync(recordId);
    }

    public async Task<ChartDataDto> GetProgressChartAsync(int goalId, int userId)
    {
        var goal = await _goalRepository.GetByIdAsync(goalId);
        if (goal == null || goal.UserId != userId)
            throw new ValidationException("Goal not found");

        var records = await _goalRepository.GetProgressRecordsByGoalIdAsync(goalId);
        var progressPercent = CalculateProgressPercent(goal, records);

        return new ChartDataDto
        {
            GoalId = goal.GoalId,
            GoalType = goal.GoalType,
            TargetValue = goal.TargetValue,
            Unit = goal.Unit,
            StartDate = goal.StartDate,
            EndDate = goal.EndDate,
            ProgressPercent = progressPercent,
            ProgressRecords = records.Select(MapToProgressDto).ToList()
        };
    }

    private decimal GetInitialValue(GoalType goalType, UserProfile? userProfile)
    {
        return goalType switch
        {
            GoalType.WeightLoss or GoalType.WeightGain or GoalType.MaintainWeight =>
                userProfile?.CurrentWeightKg ?? 0,
            GoalType.BodyMeasurement => 0, // Will be set by user in first progress record
            _ => 0
        };
    }

    private bool IsGoalCompleted(Goal goal, decimal currentValue)
    {
        return goal.GoalType switch
        {
            GoalType.WeightLoss => currentValue <= goal.TargetValue,
            GoalType.WeightGain => currentValue >= goal.TargetValue,
            GoalType.MaintainWeight => Math.Abs(currentValue - goal.TargetValue) <= 1, // Within 1kg
            GoalType.BodyMeasurement => currentValue >= goal.TargetValue, // Assuming target is achieved when reached
            _ => false
        };
    }

    private decimal CalculateProgressPercent(Goal goal, IEnumerable<ProgressRecord> records)
    {
        if (!records.Any()) return 0;

        var initialRecord = records.OrderBy(r => r.RecordDate).First();
        var latestRecord = records.OrderByDescending(r => r.RecordDate).First();

        var initialValue = initialRecord.RecordedValue;
        var currentValue = latestRecord.RecordedValue;
        var targetValue = goal.TargetValue;

        if (initialValue == targetValue) return 100;

        return goal.GoalType switch
        {
            GoalType.WeightLoss => Math.Max(0, Math.Min(100, ((initialValue - currentValue) / (initialValue - targetValue)) * 100)),
            GoalType.WeightGain => Math.Max(0, Math.Min(100, ((currentValue - initialValue) / (targetValue - initialValue)) * 100)),
            GoalType.MaintainWeight => 100, // Always 100% for maintain
            GoalType.BodyMeasurement => Math.Max(0, Math.Min(100, (currentValue / targetValue) * 100)),
            _ => 0
        };
    }

    private GoalDto MapToDto(Goal goal)
    {
        return new GoalDto
        {
            GoalId = goal.GoalId,
            GoalType = goal.GoalType,
            TargetValue = goal.TargetValue,
            Unit = goal.Unit,
            StartDate = goal.StartDate,
            EndDate = goal.EndDate,
            Status = goal.Status,
            CreatedAt = goal.CreatedAt,
            UpdatedAt = goal.UpdatedAt,
            CompletedAt = goal.CompletedAt
        };
    }

    private ProgressRecordDto MapToProgressDto(ProgressRecord record)
    {
        return new ProgressRecordDto
        {
            ProgressRecordId = record.ProgressRecordId,
            GoalId = record.GoalId,
            RecordDate = record.RecordDate,
            RecordedValue = record.RecordedValue,
            WeightKg = record.WeightKg,
            WaistCm = record.WaistCm,
            ChestCm = record.ChestCm,
            HipCm = record.HipCm,
            Notes = record.Notes,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };
    }
}