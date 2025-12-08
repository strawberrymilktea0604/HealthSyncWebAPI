using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthSync.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Notification> AddAsync(Notification notification)
    {
        _context.Notifications.Add(notification);
        return await Task.FromResult(notification);
    }

    public async Task<IEnumerable<Notification>> GetByRoleAsync(string role, int page = 1, int pageSize = 20)
    {
        var query = _context.Notifications
            .Where(n => n.RecipientRole == role)
            .OrderByDescending(n => n.CreatedAt)
            .AsQueryable();

        return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<Notification?> GetByIdAsync(int id)
    {
        return await _context.Notifications.FindAsync(id);
    }

    public async Task MarkAsReadAsync(int id)
    {
        var n = await GetByIdAsync(id);
        if (n is null) return;
        n.IsRead = true;
        _context.Notifications.Update(n);
    }

    public async Task<int> GetUnreadCountByRoleAsync(string role)
    {
        return await _context.Notifications.CountAsync(n => n.RecipientRole == role && !n.IsRead);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
