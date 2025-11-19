using System.ComponentModel.DataAnnotations;

namespace HealthSync.Application.DTOs.Auth;

/// <summary>
/// DTO cho việc khởi tạo tài khoản Admin đầu tiên
/// API này chỉ hoạt động khi chưa có Admin nào trong hệ thống
/// </summary>
public class InitializeAdminRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Full name is required")]
    [MinLength(2, ErrorMessage = "Full name must be at least 2 characters")]
    public string FullName { get; set; } = null!;

    /// <summary>
    /// Secret key để bảo vệ endpoint (lấy từ appsettings.json)
    /// </summary>
    [Required(ErrorMessage = "Initialization key is required")]
    public string InitializationKey { get; set; } = null!;
}
