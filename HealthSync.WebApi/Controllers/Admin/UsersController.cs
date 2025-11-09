using HealthSync.Application.DTOs;
using HealthSync.Application.DTOs.Users;
using HealthSync.Application.Interfaces;
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

    public UsersController(IUserRepository userRepository, IUserProfileRepository userProfileRepository)
    {
        _userRepository = userRepository;
        _userProfileRepository = userProfileRepository;
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

    [HttpPut("{id}/lock")]
    public async Task<IActionResult> LockUser([FromRoute] int id, [FromBody] LockUserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
            return NotFound(new { message = "User not found" });

        // If IsLocked == true => set IsActive = false
        var isActive = !request.IsLocked;
        await _userRepository.SetActiveStatusAsync(id, isActive);

        return NoContent();
    }
}
