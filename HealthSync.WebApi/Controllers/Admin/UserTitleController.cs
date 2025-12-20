using HealthSync.Application.DTOs.Users;
using HealthSync.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthSync.WebApi.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/users")]
[Authorize(Roles = "Admin")]
public class UserTitleController : ControllerBase
{
    private readonly IUserService _userService;

    public UserTitleController(IUserService userService)
    {
        _userService = userService;
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