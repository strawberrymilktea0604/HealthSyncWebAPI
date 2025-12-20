using HealthSync.Application.DTOs.Users;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthSync.WebApi.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Roles = "Admin")]
public class UserRankController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ILeaderboardRepository _leaderboardRepository;

    public UserRankController(IUserRepository userRepository, ILeaderboardRepository leaderboardRepository)
    {
        _userRepository = userRepository;
        _leaderboardRepository = leaderboardRepository;
    }

    [HttpPut("{id}/rank-title")]
    public async Task<IActionResult> SetUserRankTitle(int id, [FromBody] SetRankTitleRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return NotFound(new { success = false, message = "User not found" });

        var leaderboard = await _leaderboardRepository.GetByUserIdAsync(id);
        if (leaderboard == null)
        {
            // Create leaderboard entry if it doesn't exist
            leaderboard = new Leaderboard { UserId = id, TotalPoints = 0, RankTitle = request.RankTitle, UpdatedAt = DateTime.UtcNow };
            await _leaderboardRepository.AddAsync(leaderboard);
        }
        else
        {
            leaderboard.RankTitle = request.RankTitle;
            leaderboard.UpdatedAt = DateTime.UtcNow;
            await _leaderboardRepository.UpdateAsync(leaderboard);
        }

        return Ok(new { success = true, message = "User rank title updated successfully" });
    }
}