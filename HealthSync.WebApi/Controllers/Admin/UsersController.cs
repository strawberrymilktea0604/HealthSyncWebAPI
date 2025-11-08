using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using System.Threading.Tasks;

namespace HealthSync.WebApi.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly ILeaderboardRepository _leaderboardRepository;

        public UsersController(
            IUserRepository userRepository,
            IUserProfileRepository userProfileRepository,
            ILeaderboardRepository leaderboardRepository)
        {
            _userRepository = userRepository;
            _userProfileRepository = userProfileRepository;
            _leaderboardRepository = leaderboardRepository;
        }

        /// <summary>
        /// Get paginated list of users with optional search and role filter
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUsers(
            [FromQuery] string? search,
            [FromQuery] string? role,
            [FromQuery] int page = 1,
            [FromQuery] int size = 20)
        {
            try
            {
                var result = await _userRepository.GetUsersAsync(search, role, page, size);
                return Ok(new
                {
                    success = true,
                    data = result,
                    message = "Users retrieved successfully"
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while retrieving users",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get detailed information of a specific user
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "User not found"
                    });
                }

                var profile = await _userProfileRepository.GetByUserIdAsync(id);
                var leaderboard = await _leaderboardRepository.GetByUserIdAsync(id);

                var result = new
                {
                    user,
                    profile,
                    leaderboard
                };

                return Ok(new
                {
                    success = true,
                    data = result,
                    message = "User details retrieved successfully"
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while retrieving user details",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Update user active status (activate/deactivate)
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "User not found"
                    });
                }

                user.IsActive = request.IsActive;
                await _userRepository.UpdateAsync(user);

                return Ok(new
                {
                    success = true,
                    message = $"User status updated to {(request.IsActive ? "active" : "inactive")}"
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while updating user status",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Update user role
        /// </summary>
        [HttpPut("{id}/role")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateRoleRequest request)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "User not found"
                    });
                }

                user.Role = request.Role;
                await _userRepository.UpdateAsync(user);

                return Ok(new
                {
                    success = true,
                    message = $"User role updated to {request.Role}"
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while updating user role",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Update user rank title (e.g., "Top Contributor")
        /// </summary>
        [HttpPut("{id}/rank-title")]
        public async Task<IActionResult> UpdateRankTitle(int id, [FromBody] UpdateRankTitleRequest request)
        {
            try
            {
                var leaderboard = await _leaderboardRepository.GetByUserIdAsync(id);
                if (leaderboard == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Leaderboard entry not found"
                    });
                }

                leaderboard.RankTitle = request.RankTitle;
                await _leaderboardRepository.UpdateAsync(leaderboard);

                return Ok(new
                {
                    success = true,
                    message = $"Rank title updated to '{request.RankTitle}'"
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while updating rank title",
                    error = ex.Message
                });
            }
        }
    }

    // Request DTOs
    public class UpdateStatusRequest
    {
        public bool IsActive { get; set; }
    }

    public class UpdateRoleRequest
    {
        public string Role { get; set; } = string.Empty;
    }

    public class UpdateRankTitleRequest
    {
        public string? RankTitle { get; set; }
    }
}