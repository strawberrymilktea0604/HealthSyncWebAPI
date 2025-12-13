using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthSync.Infrastructure.Repositories;

public class ForumReplyRepository : IForumReplyRepository
{
    private readonly ApplicationDbContext _context;

    public ForumReplyRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Reply?> GetByIdAsync(int replyId)
    {
        return await _context.Replies
            .Include(r => r.User)
            .Include(r => r.Post)
            .FirstOrDefaultAsync(r => r.ReplyId == replyId);
    }

    public async Task<bool> ExistsAsync(int replyId)
    {
        return await _context.Replies
            .AnyAsync(r => r.ReplyId == replyId);
    }

    public async Task UpdateAsync(Reply reply)
    {
        reply.UpdatedAt = DateTime.UtcNow;
        _context.Replies.Update(reply);
        await SaveChangesAsync();
    }

    public async Task DeleteAsync(int replyId)
    {
        var reply = await _context.Replies.FindAsync(replyId);
        if (reply != null)
        {
            _context.Replies.Remove(reply);
            await SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Reply>> GetAllAsync()
    {
        return await _context.Replies.ToListAsync();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task<int> CountByUserIdAndMonthAsync(int userId, int year, int month)
    {
        return await _context.Replies
            .Where(r => r.UserId == userId &&
                       r.CreatedAt.Year == year &&
                       r.CreatedAt.Month == month)
            .CountAsync();
    }
}