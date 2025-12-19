using HealthSync.Application.DTOs.Auth;
using HealthSync.Application.Features.Auth.Interfaces;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using System.Linq;
using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace HealthSync.Application.Features.Auth.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ILeaderboardRepository _leaderboardRepository;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;

    private const string GoogleCallbackPath = "/api/v1/auth/google/callback";
    private const string GoogleTokenEndpoint = "https://oauth2.googleapis.com/token";

    public AuthService(
        IUserRepository userRepository,
        IUserProfileRepository userProfileRepository,
        ILeaderboardRepository leaderboardRepository,
        IJwtService jwtService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _userProfileRepository = userProfileRepository;
        _leaderboardRepository = leaderboardRepository;
        _jwtService = jwtService;
        _configuration = configuration;
    }

    

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("Email already exists");
        }

        var passwordHash = HashPassword(request.Password);

        var user = new ApplicationUser
        {
            Email = request.Email,
            PasswordHash = passwordHash,
            Role = "Customer",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);

        var userProfile = new UserProfile
        {
            UserId = user.UserId,
            FullName = request.FullName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _userProfileRepository.AddAsync(userProfile);

        var leaderboard = new Leaderboard
        {
            UserId = user.UserId,
            TotalPoints = 0,
            RankTitle = null,
            UpdatedAt = DateTime.UtcNow
        };
        await _leaderboardRepository.AddAsync(leaderboard);

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        var expiry = DateTime.UtcNow.AddDays(7);
        await _userRepository.SaveRefreshTokenAsync(user.UserId, refreshToken, expiry);

        return new AuthResponse(
            accessToken,
            refreshToken,
            user.UserId.ToString(),
            user.Email!,
            user.Role,
            userProfile.FullName,
            leaderboard.TotalPoints
        );
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        var expiry = DateTime.UtcNow.AddDays(7);
        await _userRepository.SaveRefreshTokenAsync(user.UserId, refreshToken, expiry);

        var userProfile = await _userProfileRepository.GetByUserIdAsync(user.UserId);
        var leaderboard = await _leaderboardRepository.GetByUserIdAsync(user.UserId);

        return new AuthResponse(
            accessToken,
            refreshToken,
            user.UserId.ToString(),
            user.Email!,
            user.Role,
            userProfile?.FullName ?? "",
            leaderboard?.TotalPoints ?? 0
        );
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        if (user.RefreshTokenExpiry < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Refresh token expired");
        }

        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        var expiry = DateTime.UtcNow.AddDays(7);
        await _userRepository.SaveRefreshTokenAsync(user.UserId, refreshToken, expiry);

        var userProfile = await _userProfileRepository.GetByUserIdAsync(user.UserId);
        var leaderboard = await _leaderboardRepository.GetByUserIdAsync(user.UserId);

        return new AuthResponse(
            accessToken,
            refreshToken,
            user.UserId.ToString(),
            user.Email!,
            user.Role,
            userProfile?.FullName ?? "",
            leaderboard?.TotalPoints ?? 0
        );
    }

    public async Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request)
    {
        try
        {
            // Validate Google token
            var payload = await GoogleJsonWebSignature.ValidateAsync(request.Token, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _configuration["GOOGLE_CLIENT_ID"] }
            });

            // Check if user exists
            var existingUser = await _userRepository.GetByEmailAsync(payload.Email);
            ApplicationUser user;

            if (existingUser != null)
            {
                // Update existing user
                user = existingUser;
                user.LastLoginAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);
            }
            else
            {
                // Create new user
                user = new ApplicationUser
                {
                    Email = payload.Email,
                    PasswordHash = string.Empty, // No password for OAuth users
                    Role = "Customer",
                    IsActive = true,
                    OauthProvider = "Google",
                    OauthProviderId = payload.Subject,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };

                await _userRepository.AddAsync(user);

                // Create user profile
                var userProfile = new UserProfile
                {
                    UserId = user.UserId,
                    FullName = payload.Name,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _userProfileRepository.AddAsync(userProfile);

                // Create leaderboard
                var leaderboard = new Leaderboard
                {
                    UserId = user.UserId,
                    TotalPoints = 0,
                    RankTitle = null,
                    UpdatedAt = DateTime.UtcNow
                };
                await _leaderboardRepository.AddAsync(leaderboard);
            }

            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            var expiry = DateTime.UtcNow.AddDays(7);
            await _userRepository.SaveRefreshTokenAsync(user.UserId, refreshToken, expiry);

            var currentUserProfile = await _userProfileRepository.GetByUserIdAsync(user.UserId);
            var currentLeaderboard = await _leaderboardRepository.GetByUserIdAsync(user.UserId);

            return new AuthResponse(
                accessToken,
                refreshToken,
                user.UserId.ToString(),
                user.Email!,
                user.Role,
                currentUserProfile?.FullName ?? payload.Name,
                currentLeaderboard?.TotalPoints ?? 0
            );
        }
        catch (Exception)
        {
            throw new UnauthorizedAccessException("Invalid Google token");
        }
    }

    private static string HashPassword(string password)
    {
        byte[] salt = new byte[128 / 8];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8));

        return $"{Convert.ToBase64String(salt)}.{hashed}";
    }

    private static bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            var parts = passwordHash.Split('.');
            if (parts.Length != 2)
                return false;

            var salt = Convert.FromBase64String(parts[0]);
            var hash = parts[1];

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));

            return hash == hashed;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Khởi tạo tài khoản Admin đầu tiên trong hệ thống
    /// API này chỉ hoạt động một lần duy nhất khi chưa có Admin nào
    /// </summary>
    public async Task<AuthResponse> InitializeAdminAsync(InitializeAdminRequest request)
    {
        // Kiểm tra xem đã có Admin chưa
        var hasAdmin = await HasAdminAccountAsync();
        if (hasAdmin)
        {
            throw new InvalidOperationException("Admin account already exists. This endpoint is disabled.");
        }

        // Kiểm tra initialization key
        var configKey = _configuration["AdminInitialization:SecretKey"];
        if (string.IsNullOrEmpty(configKey) || configKey != request.InitializationKey)
        {
            throw new UnauthorizedAccessException("Invalid initialization key");
        }

        // Kiểm tra email đã tồn tại chưa
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("Email already exists");
        }

        // Hash password
        var passwordHash = HashPassword(request.Password);

        // Tạo Admin user
        var adminUser = new ApplicationUser
        {
            Email = request.Email,
            PasswordHash = passwordHash,
            Role = "Admin", // Đặt role là Admin
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(adminUser);

        // Tạo UserProfile cho Admin
        var userProfile = new UserProfile
        {
            UserId = adminUser.UserId,
            FullName = request.FullName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _userProfileRepository.AddAsync(userProfile);

        // Tạo Leaderboard entry (mặc dù Admin không tham gia leaderboard)
        var leaderboard = new Leaderboard
        {
            UserId = adminUser.UserId,
            TotalPoints = 0,
            RankTitle = "System Administrator",
            UpdatedAt = DateTime.UtcNow
        };
        await _leaderboardRepository.AddAsync(leaderboard);

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(adminUser);
        var refreshToken = _jwtService.GenerateRefreshToken();

        var expiry = DateTime.UtcNow.AddDays(7);
        await _userRepository.SaveRefreshTokenAsync(adminUser.UserId, refreshToken, expiry);

        return new AuthResponse(
            accessToken,
            refreshToken,
            adminUser.UserId.ToString(),
            adminUser.Email!,
            adminUser.Role,
            userProfile.FullName,
            leaderboard.TotalPoints
        );
    }

    /// <summary>
    /// Kiểm tra xem hệ thống đã có Admin hay chưa
    /// </summary>
    public async Task<bool> HasAdminAccountAsync()
    {
        var allUsers = await _userRepository.GetAllAsync();
        return allUsers.Any(u => u.Role == "Admin" && u.IsActive);
    }

    public async Task<string> ExchangeGoogleCodeAsync(string code)
    {
        using var client = new HttpClient();
        
        // Get the correct redirect URI based on ASPNETCORE_URLS
        var urls = _configuration["ASPNETCORE_URLS"]?.Split(';');
        var httpsUrl = urls?.FirstOrDefault(u => u.StartsWith("https://"));
        var httpUrl = urls?.FirstOrDefault(u => u.StartsWith("http://"));
        var defaultBaseUrl = _configuration["DefaultBaseUrl"] ?? "https://localhost:7144";
        var baseUrl = httpsUrl ?? httpUrl ?? defaultBaseUrl;
        var redirectUri = $"{baseUrl}{GoogleCallbackPath}";
        
        var requestBody = new
        {
            code = code,
            client_id = _configuration["GOOGLE_CLIENT_ID"],
            client_secret = _configuration["GOOGLE_CLIENT_SECRET"],
            redirect_uri = redirectUri,
            grant_type = "authorization_code"
        };

        var response = await client.PostAsJsonAsync(GoogleTokenEndpoint, requestBody);
        response.EnsureSuccessStatusCode();
        
        var tokenResponse = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>();
        return tokenResponse?.id_token ?? throw new InvalidOperationException("Failed to get ID token from Google OAuth response");
    }
}

public class GoogleTokenResponse
{
    public string access_token { get; set; } = string.Empty;
    public string id_token { get; set; } = string.Empty;
    public string refresh_token { get; set; } = string.Empty;
    public int expires_in { get; set; }
}
