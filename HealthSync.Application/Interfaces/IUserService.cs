using HealthSync.Application.DTOs.Users;

namespace HealthSync.Application.Interfaces;

public interface IUserService
{
    Task UpdateUserStatusAsync(int userId, bool isActive);
    Task UpdateUserRoleAsync(int userId, string role);
}