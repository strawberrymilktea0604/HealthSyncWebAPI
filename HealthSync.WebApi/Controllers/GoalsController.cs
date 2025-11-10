using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthSync.Infrastructure.Data;
using HealthSync.Domain.Entities;
using HealthSync.Application.DTOs.Goals;
using Microsoft.EntityFrameworkCore;

namespace HealthSync.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GoalsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public GoalsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<ActionResult<GoalResponse>> CreateGoal([FromBody] CreateGoalRequest request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            if (request.EndDate <= request.StartDate)
            {
                return BadRequest(new { message = "EndDate must be after StartDate" });
            }

            var startDate = request.StartDate;
            var endDate = request.EndDate;

            // Get user profile to get current weight
            var userProfile = await _db.UserProfiles.FirstOrDefaultAsync(up => up.UserId == userId);
            if (userProfile == null || !userProfile.CurrentWeightKg.HasValue)
            {
                return BadRequest(new { message = "User profile with current weight is required to create a goal" });
            }

            var goal = new Goal
            {
                UserId = userId,
                GoalType = request.GoalType,
                TargetValue = request.TargetValue,
                Unit = request.Unit,
                StartDate = startDate,
                EndDate = endDate,
                Status = GoalStatus.InProgress,
                CreatedAt = DateTime.UtcNow
            };

            await _db.Goals.AddAsync(goal);

            // Create initial progress record
            var progressRecord = new ProgressRecord
            {
                GoalId = goal.GoalId, // Will be set after goal is saved
                RecordDate = startDate,
                RecordedValue = userProfile.CurrentWeightKg.Value,
                WeightKg = userProfile.CurrentWeightKg.Value,
                CreatedAt = DateTime.UtcNow
            };

            await _db.ProgressRecords.AddAsync(progressRecord);
            await _db.SaveChangesAsync();

            var response = new GoalResponse
            {
                Id = goal.GoalId,
                UserId = goal.UserId,
                GoalType = goal.GoalType,
                TargetValue = goal.TargetValue,
                Unit = goal.Unit,
                StartDate = goal.StartDate,
                EndDate = goal.EndDate,
                Status = goal.Status,
                CreatedAt = goal.CreatedAt,
                ProgressRecords = new List<ProgressRecordDto>
                {
                    new ProgressRecordDto
                    {
                        Id = progressRecord.ProgressRecordId,
                        RecordDate = progressRecord.RecordDate,
                        RecordedValue = progressRecord.RecordedValue,
                        WeightKg = progressRecord.WeightKg,
                        Notes = progressRecord.Notes,
                        CreatedAt = progressRecord.CreatedAt
                    }
                }
            };

            return CreatedAtAction(nameof(GetGoal), new { id = goal.GoalId }, response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<GoalResponse>> GetGoal(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            var goal = await _db.Goals
                .Include(g => g.ProgressRecords)
                .FirstOrDefaultAsync(g => g.GoalId == id && g.UserId == userId);
            if (goal == null)
            {
                return NotFound();
            }

            return new GoalResponse
            {
                Id = goal.GoalId,
                UserId = goal.UserId,
                GoalType = goal.GoalType,
                TargetValue = goal.TargetValue,
                Unit = goal.Unit,
                StartDate = goal.StartDate,
                EndDate = goal.EndDate,
                Status = goal.Status,
                CreatedAt = goal.CreatedAt,
                ProgressRecords = goal.ProgressRecords.Select(pr => new ProgressRecordDto
                {
                    Id = pr.ProgressRecordId,
                    GoalId = pr.GoalId,
                    RecordDate = pr.RecordDate,
                    RecordedValue = pr.RecordedValue,
                    WeightKg = pr.WeightKg,
                    WaistCm = pr.WaistCm,
                    ChestCm = pr.ChestCm,
                    HipCm = pr.HipCm,
                    Notes = pr.Notes,
                    CreatedAt = pr.CreatedAt
                }).ToList()
            };
        }

        [HttpPost("{id}/progress")]
        public async Task<ActionResult<ProgressRecordDto>> RecordProgress(int id, [FromBody] RecordProgressRequest request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            var goal = await _db.Goals.FirstOrDefaultAsync(g => g.GoalId == id && g.UserId == userId);
            if (goal == null)
            {
                return NotFound(new { message = "Goal not found" });
            }

            // Validate record date is within goal period
            if (request.RecordDate < goal.StartDate || request.RecordDate > goal.EndDate)
            {
                return BadRequest(new { message = "Record date must be within the goal period" });
            }

            // Check for duplicate record date
            var existingRecord = await _db.ProgressRecords
                .FirstOrDefaultAsync(pr => pr.GoalId == id && pr.RecordDate.Date == request.RecordDate.Date);
            if (existingRecord != null)
            {
                return BadRequest(new { message = "Progress record already exists for this date" });
            }

            var progressRecord = new ProgressRecord
            {
                GoalId = id,
                RecordDate = request.RecordDate,
                RecordedValue = request.RecordedValue,
                WeightKg = request.WeightKg,
                WaistCm = request.WaistCm,
                ChestCm = request.ChestCm,
                HipCm = request.HipCm,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _db.ProgressRecords.Add(progressRecord);

            // Check if goal is completed
            if (request.RecordedValue >= goal.TargetValue)
            {
                goal.Status = GoalStatus.Completed;
            }

            await _db.SaveChangesAsync();

            var response = new ProgressRecordDto
            {
                Id = progressRecord.ProgressRecordId,
                GoalId = progressRecord.GoalId,
                RecordDate = progressRecord.RecordDate,
                RecordedValue = progressRecord.RecordedValue,
                WeightKg = progressRecord.WeightKg,
                WaistCm = progressRecord.WaistCm,
                ChestCm = progressRecord.ChestCm,
                HipCm = progressRecord.HipCm,
                Notes = progressRecord.Notes,
                CreatedAt = progressRecord.CreatedAt
            };

            return CreatedAtAction(nameof(GetProgressRecord), new { goalId = id, recordId = progressRecord.ProgressRecordId }, response);
        }

        [HttpGet("{goalId}/progress/{recordId}")]
        public async Task<ActionResult<ProgressRecordDto>> GetProgressRecord(int goalId, int recordId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            var progressRecord = await _db.ProgressRecords
                .FirstOrDefaultAsync(pr => pr.ProgressRecordId == recordId && pr.Goal.GoalId == goalId && pr.Goal.UserId == userId);
            if (progressRecord == null)
            {
                return NotFound();
            }

            return new ProgressRecordDto
            {
                Id = progressRecord.ProgressRecordId,
                GoalId = progressRecord.GoalId,
                RecordDate = progressRecord.RecordDate,
                RecordedValue = progressRecord.RecordedValue,
                WeightKg = progressRecord.WeightKg,
                WaistCm = progressRecord.WaistCm,
                ChestCm = progressRecord.ChestCm,
                HipCm = progressRecord.HipCm,
                Notes = progressRecord.Notes,
                CreatedAt = progressRecord.CreatedAt
            };
        }
    }
}