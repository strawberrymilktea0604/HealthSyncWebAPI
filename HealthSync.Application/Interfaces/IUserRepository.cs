using HealthSync.Domain.Entities;
using HealthSync.Application.DTOs;

namespace HealthSync.Application.Interfaces;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByEmailAsync(string email);
    Task<ApplicationUser?> GetByIdAsync(int id);
    Task AddAsync(ApplicationUser user);
    Task UpdateAsync(ApplicationUser user);
    Task<ApplicationUser?> GetByRefreshTokenAsync(string refreshToken);
    Task SaveRefreshTokenAsync(int userId, string refreshToken, DateTime expiry);
    Task<PaginatedResult<ApplicationUser>> GetUsersAsync(int page, int pageSize, string? search, string? role);
    Task SetActiveStatusAsync(int userId, bool isActive);
    Task<PaginatedResult<ApplicationUser>> GetUsersAsync(string? search, string? role, int page, int size);
}