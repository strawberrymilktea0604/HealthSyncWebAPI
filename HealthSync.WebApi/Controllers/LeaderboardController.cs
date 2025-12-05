using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HealthSync.Infrastructure.Data;
using HealthSync.Application.DTOs.Leaderboard;
using System.Security.Claims;

namespace HealthSync.WebApi.Controllers;

[ApiController]
[Route("api/v1/leaderboard")]
[Authorize(Roles = "Customer")]
public class LeaderboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public LeaderboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get top users by contribution points (Customer view)
    /// </summary>
    [HttpGet("top")]
    public async Task<IActionResult> GetTopLeaderboard([FromQuery] int limit = 10)
    {
        try
        {
            if (limit < 1 || limit > 100)
            {
                return BadRequest(new { success = false, message = "Limit must be between 1 and 100" });
            }

            var leaderboard = await _context.Leaderboards
                .Include(l => l.User)
                    .ThenInclude(u => u.UserProfile)
                .OrderByDescending(l => l.TotalPoints)
                .Take(limit)
                .Select(l => new LeaderboardEntryDto
                {
                    LeaderboardId = l.LeaderboardId,
                    UserId = l.UserId,
                    UserName = l.User.UserProfile != null ? l.User.UserProfile.FullName : l.User.Email ?? "Unknown",
                    AvatarUrl = l.User.UserProfile != null ? l.User.UserProfile.AvatarUrl : null,
                    TotalPoints = l.TotalPoints,
                    RankTitle = l.RankTitle,
                    RankPosition = l.RankPosition,
                    UpdatedAt = l.UpdatedAt
                })
                .ToListAsync();

            return Ok(new { success = true, data = leaderboard });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }

    /// <summary>
    /// Get current user's leaderboard ranking
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyRanking()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { success = false, message = "Invalid user" });
            }

            var myEntry = await _context.Leaderboards
                .Include(l => l.User)
                    .ThenInclude(u => u.UserProfile)
                .FirstOrDefaultAsync(l => l.UserId == userId);

            if (myEntry == null)
            {
                return NotFound(new { success = false, message = "Leaderboard entry not found" });
            }

            // Calculate rank position if not set
            if (!myEntry.RankPosition.HasValue)
            {
                var rank = await _context.Leaderboards
                    .Where(l => l.TotalPoints > myEntry.TotalPoints)
                    .CountAsync() + 1;
                myEntry.RankPosition = rank;
            }

            var dto = new LeaderboardEntryDto
            {
                LeaderboardId = myEntry.LeaderboardId,
                UserId = myEntry.UserId,
                UserName = myEntry.User.UserProfile?.FullName ?? myEntry.User.Email ?? "Unknown",
                AvatarUrl = myEntry.User.UserProfile?.AvatarUrl,
                TotalPoints = myEntry.TotalPoints,
                RankTitle = myEntry.RankTitle,
                RankPosition = myEntry.RankPosition,
                UpdatedAt = myEntry.UpdatedAt
            };

            return Ok(new { success = true, data = dto });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }

    /// <summary>
    /// Get leaderboard with pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLeaderboard(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (pageNumber < 1)
            {
                return BadRequest(new { success = false, message = "Page number must be >= 1" });
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { success = false, message = "Page size must be between 1 and 100" });
            }

            var totalItems = await _context.Leaderboards.CountAsync();

            var leaderboard = await _context.Leaderboards
                .Include(l => l.User)
                    .ThenInclude(u => u.UserProfile)
                .OrderByDescending(l => l.TotalPoints)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new LeaderboardEntryDto
                {
                    LeaderboardId = l.LeaderboardId,
                    UserId = l.UserId,
                    UserName = l.User.UserProfile != null ? l.User.UserProfile.FullName : l.User.Email ?? "Unknown",
                    AvatarUrl = l.User.UserProfile != null ? l.User.UserProfile.AvatarUrl : null,
                    TotalPoints = l.TotalPoints,
                    RankTitle = l.RankTitle,
                    RankPosition = l.RankPosition,
                    UpdatedAt = l.UpdatedAt
                })
                .ToListAsync();

            var result = new
            {
                items = leaderboard,
                totalItems,
                pageNumber,
                pageSize,
                totalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred", error = ex.Message });
        }
    }
}
