using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthSync.Infrastructure.Data;
using HealthSync.Application.DTOs.Goals;
using HealthSync.Domain.Entities;

namespace HealthSync.WebApi.Controllers;

[ApiController]
[Route("api/v1/progress-records")]
[Authorize(Roles = "Customer")]
public class ProgressRecordsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProgressRecordsController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Create a new progress record
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateProgressRecord([FromBody] CreateProgressRecordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid input", errors = ModelState });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user" });
            }

            // Verify goal exists and belongs to user
            var goal = await _context.Goals
                .FirstOrDefaultAsync(g => g.GoalId == request.GoalId && g.UserId == userId);

            if (goal == null)
            {
                return NotFound(new { success = false, message = "Goal not found" });
            }

            // Check if record already exists for this date
            var existingRecord = await _context.ProgressRecords
                .FirstOrDefaultAsync(pr => pr.GoalId == request.GoalId && pr.RecordDate.Date == request.RecordDate.Date);

            if (existingRecord != null)
            {
                return BadRequest(new { success = false, message = "A progress record already exists for this date" });
            }

            // Validate record date is within goal period
            if (request.RecordDate.Date < goal.StartDate.Date || request.RecordDate.Date > goal.EndDate.Date)
            {
                return BadRequest(new { success = false, message = "Record date must be within goal period" });
            }

            var progressRecord = new ProgressRecord
            {
                GoalId = request.GoalId,
                RecordDate = request.RecordDate.Date,
                RecordedValue = request.RecordedValue,
                WeightKg = request.WeightKg,
                WaistCm = request.WaistCm,
                ChestCm = request.ChestCm,
                HipCm = request.HipCm,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ProgressRecords.Add(progressRecord);
            await _context.SaveChangesAsync();

            var dto = new ProgressRecordDto
            {
                ProgressRecordId = progressRecord.ProgressRecordId,
                GoalId = progressRecord.GoalId,
                RecordDate = progressRecord.RecordDate,
                RecordedValue = progressRecord.RecordedValue,
                WeightKg = progressRecord.WeightKg,
                WaistCm = progressRecord.WaistCm,
                ChestCm = progressRecord.ChestCm,
                HipCm = progressRecord.HipCm,
                Notes = progressRecord.Notes,
                CreatedAt = progressRecord.CreatedAt,
                UpdatedAt = progressRecord.UpdatedAt
            };

            return CreatedAtAction(nameof(GetProgressRecord), new { id = progressRecord.ProgressRecordId },
                new { success = true, data = dto, message = "Progress record created successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific progress record by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProgressRecord(int id)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user" });
            }

            var progressRecord = await _context.ProgressRecords
                .Include(pr => pr.Goal)
                .FirstOrDefaultAsync(pr => pr.ProgressRecordId == id && pr.Goal.UserId == userId);

            if (progressRecord == null)
            {
                return NotFound(new { success = false, message = "Progress record not found" });
            }

            var dto = new ProgressRecordDto
            {
                ProgressRecordId = progressRecord.ProgressRecordId,
                GoalId = progressRecord.GoalId,
                RecordDate = progressRecord.RecordDate,
                RecordedValue = progressRecord.RecordedValue,
                WeightKg = progressRecord.WeightKg,
                WaistCm = progressRecord.WaistCm,
                ChestCm = progressRecord.ChestCm,
                HipCm = progressRecord.HipCm,
                Notes = progressRecord.Notes,
                CreatedAt = progressRecord.CreatedAt,
                UpdatedAt = progressRecord.UpdatedAt
            };

            return Ok(new { success = true, data = dto });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }

    /// <summary>
    /// Get all progress records for a specific goal
    /// </summary>
    [HttpGet("goal/{goalId}")]
    public async Task<IActionResult> GetProgressRecordsByGoal(int goalId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user" });
            }

            // Verify goal exists and belongs to user
            var goal = await _context.Goals
                .FirstOrDefaultAsync(g => g.GoalId == goalId && g.UserId == userId);

            if (goal == null)
            {
                return NotFound(new { success = false, message = "Goal not found" });
            }

            var progressRecords = await _context.ProgressRecords
                .Where(pr => pr.GoalId == goalId)
                .OrderBy(pr => pr.RecordDate)
                .Select(pr => new ProgressRecordDto
                {
                    ProgressRecordId = pr.ProgressRecordId,
                    GoalId = pr.GoalId,
                    RecordDate = pr.RecordDate,
                    RecordedValue = pr.RecordedValue,
                    WeightKg = pr.WeightKg,
                    WaistCm = pr.WaistCm,
                    ChestCm = pr.ChestCm,
                    HipCm = pr.HipCm,
                    Notes = pr.Notes,
                    CreatedAt = pr.CreatedAt,
                    UpdatedAt = pr.UpdatedAt
                })
                .ToListAsync();

            return Ok(new { success = true, data = progressRecords });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }

    /// <summary>
    /// Update a progress record
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProgressRecord(int id, [FromBody] CreateProgressRecordRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid input", errors = ModelState });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user" });
            }

            var progressRecord = await _context.ProgressRecords
                .Include(pr => pr.Goal)
                .FirstOrDefaultAsync(pr => pr.ProgressRecordId == id && pr.Goal.UserId == userId);

            if (progressRecord == null)
            {
                return NotFound(new { success = false, message = "Progress record not found" });
            }

            // Check if changing date would create duplicate
            if (request.RecordDate.Date != progressRecord.RecordDate.Date)
            {
                var existingRecord = await _context.ProgressRecords
                    .FirstOrDefaultAsync(pr => pr.GoalId == request.GoalId && 
                                              pr.RecordDate.Date == request.RecordDate.Date &&
                                              pr.ProgressRecordId != id);

                if (existingRecord != null)
                {
                    return BadRequest(new { success = false, message = "A progress record already exists for this date" });
                }

                // Validate new record date is within goal period
                if (request.RecordDate.Date < progressRecord.Goal.StartDate.Date || 
                    request.RecordDate.Date > progressRecord.Goal.EndDate.Date)
                {
                    return BadRequest(new { success = false, message = "Record date must be within goal period" });
                }
            }

            progressRecord.RecordDate = request.RecordDate.Date;
            progressRecord.RecordedValue = request.RecordedValue;
            progressRecord.WeightKg = request.WeightKg;
            progressRecord.WaistCm = request.WaistCm;
            progressRecord.ChestCm = request.ChestCm;
            progressRecord.HipCm = request.HipCm;
            progressRecord.Notes = request.Notes;
            progressRecord.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Progress record updated successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a progress record
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProgressRecord(int id)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user" });
            }

            var progressRecord = await _context.ProgressRecords
                .Include(pr => pr.Goal)
                .FirstOrDefaultAsync(pr => pr.ProgressRecordId == id && pr.Goal.UserId == userId);

            if (progressRecord == null)
            {
                return NotFound(new { success = false, message = "Progress record not found" });
            }

            _context.ProgressRecords.Remove(progressRecord);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Progress record deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }
}
