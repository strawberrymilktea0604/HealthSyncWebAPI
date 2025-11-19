using HealthSync.Application.DTOs.Auth;
using HealthSync.Application.Features.Auth.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HealthSync.WebApi.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var response = await _authService.RegisterAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred during token refresh" });
        }
    }

    [HttpPost("google")]
    public async Task<ActionResult<AuthResponse>> GoogleLogin([FromBody] HealthSync.Application.DTOs.Auth.GoogleLoginRequest request)
    {
        try
        {
            var response = await _authService.GoogleLoginAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred during Google login" });
        }
    }

    /// <summary>
    /// API đặc biệt để khởi tạo tài khoản Admin đầu tiên
    /// ⚠️ API này chỉ hoạt động MỘT LẦN DUY NHẤT khi hệ thống chưa có Admin
    /// Sau khi đã có Admin, endpoint này sẽ tự động bị vô hiệu hóa
    /// </summary>
    /// <param name="request">Thông tin Admin và initialization key</param>
    /// <returns>Auth response với Admin token</returns>
    [HttpPost("initialize-admin")]
    public async Task<ActionResult<AuthResponse>> InitializeAdmin([FromBody] InitializeAdminRequest request)
    {
        try
        {
            // Kiểm tra xem đã có Admin chưa
            var hasAdmin = await _authService.HasAdminAccountAsync();
            if (hasAdmin)
            {
                return StatusCode(403, new 
                { 
                    success = false,
                    message = "Admin account already exists. This endpoint is permanently disabled.",
                    code = "ADMIN_ALREADY_EXISTS"
                });
            }

            var response = await _authService.InitializeAdminAsync(request);
            
            return CreatedAtAction(nameof(InitializeAdmin), new 
            { 
                success = true,
                message = "Admin account created successfully. This endpoint is now disabled.",
                data = response 
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new 
            { 
                success = false,
                message = ex.Message,
                code = "INVALID_INITIALIZATION_KEY"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new 
            { 
                success = false,
                message = ex.Message,
                code = "INITIALIZATION_ERROR"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new 
            { 
                success = false,
                message = "An error occurred during admin initialization",
                detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Kiểm tra xem hệ thống đã có Admin hay chưa (public endpoint)
    /// Endpoint này giúp client biết liệu có thể gọi initialize-admin hay không
    /// </summary>
    [HttpGet("admin-status")]
    public async Task<ActionResult> CheckAdminStatus()
    {
        try
        {
            var hasAdmin = await _authService.HasAdminAccountAsync();
            
            return Ok(new 
            { 
                success = true,
                hasAdmin = hasAdmin,
                initializationAvailable = !hasAdmin,
                message = hasAdmin 
                    ? "Admin account exists. System is ready." 
                    : "No admin account. System requires initialization."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new 
            { 
                success = false,
                message = "An error occurred while checking admin status",
                detail = ex.Message
            });
        }
    }
}