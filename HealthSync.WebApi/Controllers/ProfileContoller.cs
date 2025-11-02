using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthSync.Application.Interfaces;
using HealthSync.Application.DTOs.Users;

namespace HealthSync.WebApi.Controllers;
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IUserProfileService _profileService;

        public ProfileController(IUserProfileService profileService)
        {
            _profileService = profileService;
        }

        
        /// <summary>
        /// Gets the profile of the currently authenticated user
        /// </summary>
        /// <returns>The user's profile information</returns>
        /// <response code="200">Returns the user profile</response>
        /// <response code="401">If the JWT token is missing or invalid</response>
        /// <response code="404">If the user profile was not found</response>
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var id))
            {
                return Unauthorized(new { success = false, message = "User ID not found in token" });
            }

            var profile = await _profileService.GetUserProfileAsync(id);
            if (profile == null)
            {
                return NotFound(new { success = false, message = "Profile not found" });
            }

            return Ok(new { success = true, data = profile });
        }
    }
}
