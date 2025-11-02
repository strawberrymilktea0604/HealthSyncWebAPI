using HealthSync.Application.DTOs.Auth;
using HealthSync.Application.Features.Auth.Interfaces;
using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace HealthSync.Application.Features.Auth.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly ILeaderboardRepository _leaderboardRepository;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;

    public AuthService(
        IUserRepository userRepository,
        IUserProfileRepository userProfileRepository,
        ILeaderboardRepository leaderboardRepository,
        IJwtService jwtService,
        IPasswordHasher<ApplicationUser> passwordHasher)
    {
        _userRepository = userRepository;
        _userProfileRepository = userProfileRepository;
        _leaderboardRepository = leaderboardRepository;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Check if email already exists
        if (await _userRepository.EmailExistsAsync(request.Email))
        {
            throw new InvalidOperationException("Email already exists");
        }

        // Create user
        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.Email,
            Role = "Customer", // Default role
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Hash password
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        await _userRepository.AddAsync(user);

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

        return new AuthResponse(accessToken, refreshToken, user.Id.ToString(), user.Email!, user.Role);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        // Verify password
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash!, request.Password);
        if (result != PasswordVerificationResult.Success)
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = _jwtService.GenerateRefreshToken();

        return new AuthResponse(accessToken, refreshToken, user.Id.ToString(), user.Email!, user.Role);
    }
}