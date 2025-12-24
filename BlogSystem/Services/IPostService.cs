using BlogSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BlogSystem.Services;

public interface IPostService
{
    Task<Post> AddAsync(
        string title,
        string content,
        Guid userId);

    Task<IEnumerable<Post>> GetAsync(
        int number,
        int size);

    Task<Post> GetByIdAsync(Guid id);

    Task UpdateByIdAsync(
        Guid id,
        string title,
        string content);

    Task RemoveByIdAsync(Guid id);
}

public class PostService : IPostService
{
    private readonly AppDbContext _context;

    public PostService(
        AppDbContext context)
    {
        _context = context;
    }

    public async Task<Post> AddAsync(
        string title, 
        string content, 
        Guid userId)
    {
        bool exists = await _context.Users
            .AnyAsync(u => u.Id == userId);

        if (!exists)
            throw new KeyNotFoundException();

        Post post = new()
        {
            Title = title,
            Content = content,
            UserId = userId,
        };

        await _context.AddAsync(post);
        await _context.SaveChangesAsync();

        return post;
    }

    public async Task<IEnumerable<Post>> GetAsync(int number, int size)
    {
        IEnumerable<Post> posts = await _context.Posts
            .AsNoTracking()
            .OrderBy(p => p.CreatedAt)
            .Skip((number - 1) * size)
            .Take(size)
            .ToListAsync();

        return posts;
    }

    public async Task<Post> GetByIdAsync(Guid id)
    {
        Post? post = await _context.Posts
            .Include(p => p.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        if (post is null)
            throw new KeyNotFoundException();

        return post;
    }

    public async Task UpdateByIdAsync(
        Guid id,
        string title,
        string content)
    {
        int result = await _context.Posts
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Title, title)
                .SetProperty(p => p.Content, content));

        if (result == 0)
            throw new KeyNotFoundException();
    }

    public async Task RemoveByIdAsync(Guid id)
    {
        int result = await _context.Posts
            .Where(p => p.Id == id)
            .ExecuteDeleteAsync();

        if (result == 0)
            throw new KeyNotFoundException();
    }
}
