using HealthSync.Application.DTOs.Users;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthSync.WebApi.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Roles = "Admin")]
public class UserStatusController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public UserStatusController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
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
}