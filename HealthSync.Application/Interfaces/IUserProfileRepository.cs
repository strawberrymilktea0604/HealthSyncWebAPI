using HealthSync.Domain.Entities;

namespace HealthSync.Application.Interfaces;

public interface IUserProfileRepository
{
    Task AddAsync(UserProfile userProfile);
    Task<UserProfile?> GetByUserIdAsync(int userId);
    Task UpdateAsync(UserProfile userProfile);
}