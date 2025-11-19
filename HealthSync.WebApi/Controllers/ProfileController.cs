using System.Security.Claims;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthSync.WebApi.Controllers;

[ApiController]
[Route("api/v1/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IStorageService _storageService;
    private readonly IUserProfileRepository _userProfileRepository;

    public ProfileController(IStorageService storageService, IUserProfileRepository userProfileRepository)
    {
        _storageService = storageService;
        _userProfileRepository = userProfileRepository;
    }

    [HttpPost("avatar")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded" });

        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idClaim, out var userId) || userId <= 0)
            return Unauthorized(new { message = "Invalid user" });

        // create a filename with user id to make it identifiable
        var ext = Path.GetExtension(file.FileName);
        var fileName = $"avatar_{userId}{ext}";

        var url = await _storageService.UploadAsync(file, "avatars", fileName);

        var profile = await _userProfileRepository.GetByUserIdAsync(userId);
        if (profile == null)
        {
            profile = new UserProfile
            {
                UserId = userId,
                FullName = "",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                AvatarUrl = url
            };
            await _userProfileRepository.AddAsync(profile);
        }
        else
        {
            profile.AvatarUrl = url;
            profile.UpdatedAt = DateTime.UtcNow;
            await _userProfileRepository.UpdateAsync(profile);
        }

        return Ok(new { avatarUrl = url });
    }
}
