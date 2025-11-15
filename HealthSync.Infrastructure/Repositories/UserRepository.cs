using HealthSync.Application.Interfaces;
using HealthSync.Application.DTOs;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthSync.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email)
    {
        return await _context.ApplicationUsers.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<ApplicationUser?> GetByIdAsync(int id)
    {
        return await _context.ApplicationUsers.FindAsync(id);
    }

    public async Task AddAsync(ApplicationUser user)
    {
        await _context.ApplicationUsers.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ApplicationUser user)
    {
        _context.ApplicationUsers.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.ApplicationUsers.AnyAsync(u => u.Email == email);
    }

    public async Task<ApplicationUser?> GetByRefreshTokenAsync(string refreshToken)
    {
        return await _context.ApplicationUsers.FirstOrDefaultAsync(u =>
            u.RefreshToken == refreshToken &&
            u.RefreshTokenExpiry > DateTime.UtcNow);
    }

    public async Task SaveRefreshTokenAsync(int userId, string refreshToken, DateTime expiry)
    {
        var user = await _context.ApplicationUsers.FindAsync(userId);
        if (user != null)
        {
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = expiry;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<PaginatedResult<ApplicationUser>> GetUsersAsync(int page, int size, string? search, string? role)
    {
        var query = _context.ApplicationUsers
            .Include(u => u.UserProfile)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u => u.Email.Contains(search) || 
                                   (u.UserProfile != null && u.UserProfile.FullName.Contains(search)));
        }

        if (!string.IsNullOrEmpty(role))
        {
            query = query.Where(u => u.Role == role);
        }

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderBy(u => u.UserId)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalItems / (double)size);

        return new PaginatedResult<ApplicationUser>
        {
            Items = items,
            CurrentPage = page,
            PageSize = size,
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }

    public async Task SetActiveStatusAsync(int userId, bool isActive)
    {
        var user = await _context.ApplicationUsers.FindAsync(userId);
        if (user == null) return;

        user.IsActive = isActive;
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetTotalWorkoutsAsync(int userId)
    {
        return await _context.WorkoutLogs.CountAsync(w => w.UserId == userId);
    }

    public async Task<int> GetTotalNutritionLogsAsync(int userId)
    {
        return await _context.NutritionLogs.CountAsync(n => n.UserId == userId);
    }

    public async Task<int> GetTotalGoalsAsync(int userId)
    {
        return await _context.Goals.CountAsync(g => g.UserId == userId);
    }

    public async Task<int> GetTotalChallengesAsync(int userId)
    {
        return await _context.ChallengeParticipations.CountAsync(cp => cp.UserId == userId);
    }
}