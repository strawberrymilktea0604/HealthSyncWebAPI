using System.Collections.Generic;

namespace HealthSync.Application.DTOs.Goals;

public class UserProgressChartDto
{
    public List<ProgressPointDto> ProgressPoints { get; set; } = new();
}