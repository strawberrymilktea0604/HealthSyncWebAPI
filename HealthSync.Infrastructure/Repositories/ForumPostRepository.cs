using HealthSync.Application.Interfaces;
using HealthSync.Domain.Entities;
using HealthSync.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthSync.Infrastructure.Repositories;

public class ForumPostRepository : IForumPostRepository
{
    private readonly ApplicationDbContext _context;

    public ForumPostRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Post?> GetByIdWithRepliesAsync(int postId)
    {
        return await _context.Posts
            .Include(p => p.Replies)
            .Include(p => p.User)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.PostId == postId);
    }

    public async Task<Post?> GetByIdAsync(int postId)
    {
        return await _context.Posts
            .FirstOrDefaultAsync(p => p.PostId == postId);
    }

    public async Task<bool> ExistsAsync(int postId)
    {
        return await _context.Posts
            .AnyAsync(p => p.PostId == postId);
    }

    public async Task<Post> AddAsync(Post post)
    {
        await _context.Posts.AddAsync(post);
        await SaveChangesAsync();
        return post;
    }

    public async Task UpdateAsync(Post post)
    {
        post.UpdatedAt = DateTime.UtcNow;
        _context.Posts.Update(post);
        await SaveChangesAsync();
    }

    public async Task DeleteAsync(int postId)
    {
        var post = await GetByIdAsync(postId);
        if (post is not null)
        {
            _context.Posts.Remove(post);
            await SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Post>> GetAllPostsAsync()
    {
        return await _context.Posts.ToListAsync();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task<int> CountByUserIdAndMonthAsync(int userId, int year, int month)
    {
        return await _context.Posts
            .Where(p => p.UserId == userId &&
                       p.CreatedAt.Year == year &&
                       p.CreatedAt.Month == month)
            .CountAsync();
    }
}
