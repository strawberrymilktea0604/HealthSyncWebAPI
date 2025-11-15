using HealthSync.Application.DTOs.Goals;

namespace HealthSync.Application.Interfaces;

public interface IGoalService
{
    Task<GoalDto> CreateGoalAsync(CreateGoalRequest request, int userId);
    Task<IEnumerable<GoalDto>> GetUserGoalsAsync(int userId);
    Task<GoalDto> GetGoalByIdAsync(int goalId, int userId);
    Task<GoalDto> UpdateGoalAsync(int goalId, UpdateGoalRequest request, int userId);
    Task DeleteGoalAsync(int goalId, int userId);
    Task<ProgressRecordDto> RecordProgressAsync(RecordProgressRequest request, int userId);
    Task<ProgressRecordDto> UpdateProgressRecordAsync(int recordId, UpdateProgressRequest request, int userId);
    Task DeleteProgressRecordAsync(int recordId, int userId);
    Task<ChartDataDto> GetProgressChartAsync(int goalId, int userId);
}