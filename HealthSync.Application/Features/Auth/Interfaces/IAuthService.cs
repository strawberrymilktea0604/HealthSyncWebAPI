using HealthSync.Application.DTOs.Auth;

namespace HealthSync.Application.Features.Auth.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request);
    
    /// <summary>
    /// Khởi tạo tài khoản Admin đầu tiên trong hệ thống
    /// Chỉ hoạt động khi chưa có Admin nào
    /// </summary>
    Task<AuthResponse> InitializeAdminAsync(InitializeAdminRequest request);
    
    /// <summary>
    /// Kiểm tra xem hệ thống đã có Admin chưa
    /// </summary>
    Task<bool> HasAdminAccountAsync();
}