using HealthSync.Domain.Entities;

namespace HealthSync.Application.Interfaces;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByEmailAsync(string email);
    Task<ApplicationUser?> GetByIdAsync(int id);
    Task AddAsync(ApplicationUser user);
    Task UpdateAsync(ApplicationUser user);
    Task<ApplicationUser?> GetByRefreshTokenAsync(string refreshToken);
    Task SaveRefreshTokenAsync(int userId, string refreshToken, DateTime expiry);
}