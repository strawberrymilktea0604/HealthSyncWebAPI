using System;
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

            if (request.EndDate.HasValue && request.StartDate.HasValue && request.EndDate.Value <= request.StartDate.Value)
            {
                return BadRequest(new { message = "EndDate must be after StartDate" });
            }

            var startDate = request.StartDate ?? DateTime.UtcNow;
            var endDate = request.EndDate ?? startDate.AddMonths(1);

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
                CreatedAt = goal.CreatedAt
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

            var goal = await _db.Goals.FirstOrDefaultAsync(g => g.GoalId == id && g.UserId == userId);
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
                CreatedAt = goal.CreatedAt
            };
        }
    }
}