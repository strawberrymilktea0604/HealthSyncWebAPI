using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HealthSync.Application.Interfaces;
using HealthSync.Application.DTOs.Users;

namespace HealthSync.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Customer")]
public class UsersController : ControllerBase
{
    private readonly IUserProfileService _profileService;
    private readonly IFileStorageService _fileStorageService;

    public UsersController(IUserProfileService profileService, IFileStorageService fileStorageService)
    {
        _profileService = profileService;
        _fileStorageService = fileStorageService;
    }

    private const string UserIdNotFoundMessage = "User ID not found in token";

    /// <summary>
    /// Gets the profile of the currently authenticated user
    /// </summary>
    /// <returns>The user's profile information</returns>
    /// <response code="200">Returns the user profile</response>
    /// <response code="401">If the JWT token is missing or invalid</response>
    /// <response code="404">If the user profile was not found</response>
    [HttpGet("profile")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var id))
        {
            return Unauthorized(new { success = false, message = UserIdNotFoundMessage });
        }

        var profile = await _profileService.GetUserProfileResponseAsync(id);
        if (profile == null)
        {
            return NotFound(new { success = false, message = "Profile not found" });
        }

        return Ok(new { success = true, data = profile });
    }

    /// <summary>
    /// Updates the profile of the currently authenticated user
    /// </summary>
    /// <param name="request">The profile update information</param>
    /// <returns>The updated user profile</returns>
    /// <response code="200">Returns the updated profile</response>
    /// <response code="400">If the request model is invalid</response>
    /// <response code="401">If the JWT token is missing or invalid</response>
    [HttpPut("profile")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserProfileRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, errors = ModelState });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var id))
        {
            return Unauthorized(new { success = false, message = UserIdNotFoundMessage });
        }

        var updatedProfile = await _profileService.UpdateUserProfileAsync(request, id);
        return Ok(new { success = true, data = updatedProfile });
    }

    /// <summary>
    /// Uploads or updates the user's avatar
    /// </summary>
    /// <param name="file">The image file to upload</param>
    /// <returns>The URL of the uploaded avatar</returns>
    /// <response code="200">Returns the avatar URL</response>
    /// <response code="400">If no file was uploaded or file is invalid</response>
    /// <response code="401">If the JWT token is missing or invalid</response>
    [HttpPost("avatar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadAvatar(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "No file uploaded" });

        // Validate file size (< 5MB)
        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { success = false, message = "File size must be less than 5MB" });

        // Validate MIME type (image/jpeg, image/png)
        var allowedTypes = new[] { "image/jpeg", "image/png" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequest(new { success = false, message = "Only JPEG and PNG images are allowed" });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var id))
        {
            return Unauthorized(new { success = false, message = UserIdNotFoundMessage });
        }

        // Get current profile to retrieve old avatar URL
        var profile = await _profileService.GetUserProfileAsync(id);
        string? oldAvatarUrl = profile?.AvatarUrl;

        // Upload new avatar
        var imageUrl = await _fileStorageService.UploadAsync(file, "avatars");

        // Update profile with new avatar URL
        await _profileService.UpdateAvatarAsync(id, imageUrl);

        // Delete old avatar if exists
        if (!string.IsNullOrEmpty(oldAvatarUrl))
        {
            // Extract object name from URL (assuming format: scheme://endpoint/bucket/objectName)
            // e.g., https://localhost:9000/healthsync-images/avatars/guid.jpg -> avatars/guid.jpg
            var uri = new Uri(oldAvatarUrl);
            var segments = uri.AbsolutePath.TrimStart('/').Split('/');
            if (segments.Length >= 2)
            {
                var objectName = string.Join("/", segments.Skip(1)); // Skip bucket name
                await _fileStorageService.DeleteFileAsync(objectName);
            }
        }

        return Ok(new { success = true, data = new { avatarUrl = imageUrl } });
    }

    /// <summary>
    /// Gets the fitness and nutrition stats of the currently authenticated user
    /// </summary>
    /// <returns>The user's aggregated statistics</returns>
    /// <response code="200">Returns the user's stats</response>
    /// <response code="401">If the JWT token is missing or invalid</response>
    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyStats()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var id))
        {
            return Unauthorized(new { success = false, message = UserIdNotFoundMessage });
        }

        var stats = await _profileService.GetUserStatsAsync(id);
        return Ok(new { success = true, data = stats });
    }
}