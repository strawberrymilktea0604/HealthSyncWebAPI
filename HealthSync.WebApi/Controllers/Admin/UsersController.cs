using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.Users;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthSync.WebApi.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ILeaderboardRepository _leaderboardRepository;

    public UsersController(IUserRepository userRepository, IUserProfileRepository userProfileRepository, ILeaderboardRepository leaderboardRepository)
    {
        _userRepository = userRepository;
        _userProfileRepository = userProfileRepository;
        _leaderboardRepository = leaderboardRepository;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<AdminUserDto>>> GetUsers([FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] string? search = null, [FromQuery] string? role = null)
    {
        if (page < 1) page = 1;
        if (size < 1) size = 20;

        var usersPaged = await _userRepository.GetUsersAsync(page, size, search, role);

        var items = new List<AdminUserDto>();
        foreach (var u in usersPaged.Items)
        {
            var profile = await _userProfileRepository.GetByUserIdAsync(u.UserId);
            items.Add(new AdminUserDto(u.UserId, u.Email ?? "", u.Role, u.IsActive, profile?.FullName ?? "", u.CreatedAt));
        }

        var result = new PaginatedResult<AdminUserDto>
        {
            Items = items,
            CurrentPage = usersPaged.CurrentPage,
            PageSize = usersPaged.PageSize,
            TotalItems = usersPaged.TotalItems,
            TotalPages = usersPaged.TotalPages
        };

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AdminUserDetailsDto>> GetUserDetails(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return NotFound(new { success = false, message = "User not found" });

        var userProfile = await _userProfileRepository.GetByUserIdAsync(id);
        var leaderboard = await _leaderboardRepository.GetByUserIdAsync(id);

        // Calculate stats (this could be moved to a service for better separation)
        var totalWorkouts = await _userRepository.GetTotalWorkoutsAsync(id);
        var totalNutritionLogs = await _userRepository.GetTotalNutritionLogsAsync(id);
        var totalGoals = await _userRepository.GetTotalGoalsAsync(id); // Assuming this method exists or will be added
        var totalChallenges = await _userRepository.GetTotalChallengesAsync(id); // Assuming this method exists or will be added

        var userDto = new AdminUserDto(
            Id: user.UserId,
            Email: user.Email,
            Role: user.Role,
            IsActive: user.IsActive,
            FullName: userProfile?.FullName ?? string.Empty,
            CreatedAt: user.CreatedAt
        );

        var profileDto = userProfile != null ? new UserProfileDto(
            UserProfileId: userProfile.UserProfileId,
            UserId: userProfile.UserId,
            FullName: userProfile.FullName,
            Gender: userProfile.Gender?.ToString(),
            DateOfBirth: userProfile.DateOfBirth,
            HeightCm: userProfile.HeightCm,
            CurrentWeightKg: userProfile.CurrentWeightKg,
            ActivityLevel: userProfile.ActivityLevel?.ToString(),
            AvatarUrl: userProfile.AvatarUrl,
            ContributionPoints: leaderboard?.TotalPoints ?? 0,
            CreatedAt: userProfile.CreatedAt,
            UpdatedAt: userProfile.UpdatedAt
        ) : null;

        var statsDto = new UserStatsDto(
            TotalWorkouts: totalWorkouts,
            TotalNutritionLogs: totalNutritionLogs,
            TotalGoals: totalGoals,
            TotalChallenges: totalChallenges,
            ContributionPoints: leaderboard?.TotalPoints ?? 0
        );

        var details = new AdminUserDetailsDto(
            User: userDto,
            Profile: profileDto,
            Stats: statsDto
        );

        return Ok(new { success = true, data = details });
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> SetUserStatus([FromRoute] int id, [FromBody] SetActiveRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return NotFound(new { message = "User not found" });

        await _userRepository.SetActiveStatusAsync(id, request.IsActive);

        return NoContent();
    }

    [HttpPut("{id}/role")]
    public async Task<IActionResult> SetUserRole(int id, [FromBody] SetRoleRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return NotFound(new { success = false, message = "User not found" });

        user.Role = request.Role;
        await _userRepository.UpdateAsync(user);

        return Ok(new { success = true, message = "User role updated successfully" });
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
