using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
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
            // Get user id from JWT claims
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            var start = request.StartDate ?? DateTime.UtcNow;
            var end = request.EndDate ?? start.AddMonths(1);
            if (end <= start)
                return BadRequest(new { message = "EndDate must be after StartDate" });

            var now = DateTime.UtcNow;
            var goal = new Goal
            {
                UserId = userId,
                GoalType = request.GoalType,
                TargetValue = request.TargetValue,
                StartDate = start,
                EndDate = end,
                Status = "Active",
                Description = request.Description,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _db.Goals.AddAsync(goal);
            await _db.SaveChangesAsync();

            var response = new GoalResponse
            {
                Id = goal.GoalId,
                UserId = goal.UserId,
                GoalType = goal.GoalType,
                TargetValue = goal.TargetValue,
                StartDate = goal.StartDate,
                EndDate = goal.EndDate,
                Status = goal.Status,
                Description = request.Description
            };

            // Return created with location header
            return Created($"/api/goals/{goal.GoalId}", response);
        }

    }
}
