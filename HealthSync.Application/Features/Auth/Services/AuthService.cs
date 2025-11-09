using HealthSync.Application.DTOs.Auth;
using HealthSync.Application.Features.Auth.Interfaces;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;

namespace HealthSync.Application.Features.Auth.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IUserRepository _userRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ILeaderboardRepository _leaderboardRepository;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IUserRepository userRepository,
        IUserProfileRepository userProfileRepository,
        ILeaderboardRepository leaderboardRepository,
        IJwtService jwtService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _userRepository = userRepository;
        _userProfileRepository = userProfileRepository;
        _leaderboardRepository = leaderboardRepository;
        _jwtService = jwtService;
        _configuration = configuration;
    }

    public async Task<AuthResponse> GoogleLoginAsync(GoogleLoginRequest request)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new[] { _configuration["Authentication:Google:ClientId"] ?? _configuration["Google:ClientId"] }
            };

            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
        }
        catch (Exception)
        {
            throw new UnauthorizedAccessException("Invalid Google token");
        }

        var email = payload.Email!;
        var name = payload.Name ?? payload.Email!;
        var providerId = payload.Subject;

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                Email = email,
                UserName = email,
                Role = "Customer",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                OauthProvider = "Google",
                OauthProviderId = providerId
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
                throw new InvalidOperationException($"Failed to create user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");

            // Create user profile
            var userProfile = new UserProfile
            {
                UserId = user.Id,
                FullName = name,
                CreatedAt = DateTime.UtcNow
            };
            await _userProfileRepository.AddAsync(userProfile);

            // Create leaderboard entry
            var leaderboard = new Leaderboard
            {
                UserId = user.Id,
                TotalPoints = 0,
                RankTitle = null
            };
            await _leaderboardRepository.AddAsync(leaderboard);
        }
        else
        {
            // Update oauth fields if missing
            if (string.IsNullOrEmpty(user.OauthProvider))
            {
                user.OauthProvider = "Google";
                user.OauthProviderId = providerId;
                await _userRepository.UpdateAsync(user);
            }
        }

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Save refresh token
        var expiry = DateTime.UtcNow.AddDays(7);
        await _userRepository.SaveRefreshTokenAsync(user.Id, refreshToken, expiry);

        var userProfileResp = await _userProfileRepository.GetByUserIdAsync(user.Id);
        var leaderboardResp = await _leaderboardRepository.GetByUserIdAsync(user.Id);

        return new AuthResponse(
            accessToken,
            refreshToken,
            user.Id.ToString(),
            user.Email!,
            user.Role,
            userProfileResp?.FullName ?? name,
            leaderboardResp?.TotalPoints ?? 0
        );
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("Email already exists");
        }

        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.Email,
            Role = "Customer", // Default role
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // Create user profile
        var userProfile = new UserProfile
        {
            UserId = user.Id,
            FullName = request.FullName,
            CreatedAt = DateTime.UtcNow
        };
        await _userProfileRepository.AddAsync(userProfile);

        // Create leaderboard entry
        var leaderboard = new Leaderboard
        {
            UserId = user.Id,
            TotalPoints = 0,
            RankTitle = null
        };
        await _leaderboardRepository.AddAsync(leaderboard);

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        return new AuthResponse(
            accessToken,
            refreshToken,
            user.Id.ToString(),
            user.Email!,
            user.Role,
            userProfile.FullName,
            leaderboard.TotalPoints
        );
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Get user profile and leaderboard for response
        var userProfile = await _userProfileRepository.GetByUserIdAsync(user.Id);
        var leaderboard = await _leaderboardRepository.GetByUserIdAsync(user.Id);

        return new AuthResponse(
            accessToken,
            refreshToken,
            user.Id.ToString(),
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

        // Generate new tokens
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Update refresh token in database
        var expiry = DateTime.UtcNow.AddDays(7);
        await _userRepository.SaveRefreshTokenAsync(user.Id, refreshToken, expiry);

        // Get user profile and leaderboard for response
        var userProfile = await _userProfileRepository.GetByUserIdAsync(user.Id);
        var leaderboard = await _leaderboardRepository.GetByUserIdAsync(user.Id);

        return new AuthResponse(
            accessToken,
            refreshToken,
            user.Id.ToString(),
            user.Email!,
            user.Role,
            userProfile?.FullName ?? "",
            leaderboard?.TotalPoints ?? 0
        );
    }
}