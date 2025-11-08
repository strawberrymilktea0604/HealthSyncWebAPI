using HealthSync.Application.DTOs.Auth;
using HealthSync.Application.Features.Auth.Interfaces;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Google.Apis.Auth;

namespace HealthSync.Application.Features.Auth.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ILeaderboardRepository _leaderboardRepository;
    private readonly IJwtService _jwtService;

    public AuthService(
        IUserRepository userRepository,
        IUserProfileRepository userProfileRepository,
        ILeaderboardRepository leaderboardRepository,
        IJwtService jwtService)
    {
        _userRepository = userRepository;
        _userProfileRepository = userProfileRepository;
        _leaderboardRepository = leaderboardRepository;
        _jwtService = jwtService;
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
                Audience = new[] { Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") }
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
                    PasswordHash = null, // No password for OAuth users
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
        catch (Exception ex)
        {
            throw new UnauthorizedAccessException("Invalid Google token");
        }
    }

    private string HashPassword(string password)
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
            iterationCount: 10000,
            numBytesRequested: 256 / 8));

        return $"{Convert.ToBase64String(salt)}.{hashed}";
    }

    private bool VerifyPassword(string password, string passwordHash)
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
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return hash == hashed;
        }
        catch
        {
            return false;
        }
    }
}
