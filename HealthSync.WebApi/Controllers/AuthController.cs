using HealthSync.Application.DTOs.Auth;
using HealthSync.Application.Features.Auth.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace HealthSync.WebApi.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    public AuthController(IAuthService authService, IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
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

    [HttpGet("google/login")]
    public ActionResult GoogleLoginPage()
    {
        var clientId = _configuration["GOOGLE_CLIENT_ID"];
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var redirectUri = $"{baseUrl}/api/v1/auth/google/callback";
        var scope = "openid email profile";
        var responseType = "code";
        
        var authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                     $"client_id={clientId}&" +
                     $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                     $"scope={Uri.EscapeDataString(scope)}&" +
                     $"response_type={responseType}&" +
                     $"access_type=offline";

        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>HealthSync - Google Login</title>
    <style>
        body {{ font-family: Arial, sans-serif; text-align: center; padding: 50px; }}
        .login-btn {{ 
            background: #4285f4; 
            color: white; 
            padding: 12px 24px; 
            border: none; 
            border-radius: 4px; 
            font-size: 16px; 
            cursor: pointer; 
            text-decoration: none; 
            display: inline-block; 
        }}
        .login-btn:hover {{ background: #3367d6; }}
    </style>
</head>
<body>
    <h1>HealthSync Google Login</h1>
    <p>Click the button below to login with Google</p>
    <a href=""{authUrl}"" class=""login-btn"">Login with Google</a>
    <p><small>After login, you'll be redirected back with your tokens</small></p>
</body>
</html>";

        return Content(html, "text/html");
    }

    [HttpGet("google/callback")]
    public async Task<ActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string? error = null)
    {
        if (!string.IsNullOrEmpty(error))
        {
            return BadRequest(new { message = $"OAuth error: {error}" });
        }
        
        if (string.IsNullOrEmpty(code))
        {
            return BadRequest(new { message = "No authorization code received" });
        }

        try
        {
            var idToken = await _authService.ExchangeGoogleCodeAsync(code);
            return Content($@"
<html>
<body>
<h2>Google Authorization Successful!</h2>
<p>Authorization completed successfully. You can now use the token to login.</p>
</body>
</html>", "text/html");
        }
        catch (Exception ex)
        {
            return Content($@"
<html>
<body>
<h2>Error exchanging code</h2>
<p>{ex.Message}</p>
</body>
</html>", "text/html");
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