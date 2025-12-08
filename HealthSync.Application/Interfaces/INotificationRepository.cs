using HealthSync.Domain.Entities;

namespace HealthSync.Application.Interfaces;

public interface INotificationRepository
{
    Task<Notification> AddAsync(Notification notification);
    Task<IEnumerable<Notification>> GetByRoleAsync(string role, int page = 1, int pageSize = 20);
    Task<Notification?> GetByIdAsync(int id);
    Task MarkAsReadAsync(int id);
    Task<int> GetUnreadCountByRoleAsync(string role);
    Task<int> SaveChangesAsync();
}
