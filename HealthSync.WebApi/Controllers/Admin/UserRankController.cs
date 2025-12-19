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
    private readonly IUserService _userService;

    public UserRankController(IUserRepository userRepository, ILeaderboardRepository leaderboardRepository, IUserService userService)
    {
        _userRepository = userRepository;
        _leaderboardRepository = leaderboardRepository;
        _userService = userService;
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

    [HttpPost("{id}/set-title")]
    public async Task<IActionResult> SetUserTitle(int id, [FromBody] SetRankTitleRequest request)
    {
        try
        {
            var result = await _userService.SetUserRankTitleAsync(id, request.RankTitle);
            return Ok(new { success = true, data = result, message = "User rank title set successfully" });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { success = false, message = "User not found" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}