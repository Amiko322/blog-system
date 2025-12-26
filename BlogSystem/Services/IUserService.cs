using BlogSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BlogSystem.Services;

public interface IUserService
{
    Task<User> AddAsync(
        string login,
        string passwordHash,
        string lastName,
        string firstName);

    Task<IEnumerable<User>> GetAsync(
        int number,
        int size);

    Task<User> GetByIdAsync(Guid id);

    Task<User> GetByLoginAsync(string login);

    Task UpdateByIdAsync(
        Guid id,
        string login,
        string lastName,
        string firstName);

    Task RemoveByIdAsync(Guid id);
}

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(
        AppDbContext context)
    {
        _context = context;
    }

    public async Task<User> AddAsync(
        string login,
        string passwordHash,
        string lastName,
        string firstName)
    {
        bool exists = await _context.Users
            .AnyAsync(u => u.Login == login);

        if (exists)
            throw new InvalidOperationException();

        User user = new()
        {
            Login = login,
            PasswordHash = passwordHash,
            LastName = lastName,
            FirstName = firstName,
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        return user;
    }

    public async Task<IEnumerable<User>> GetAsync(
        int number,
        int size)
    {
        IEnumerable<User> users = await _context.Users 
            .AsNoTracking()
            .OrderBy(u => u.RegisteredAt)
            .Skip((number - 1) * size)
            .Take(size)
            .ToListAsync();

        return users;
    }

    public async Task<User> GetByIdAsync(Guid id)
    {
        User? user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
            throw new KeyNotFoundException();

        return user;
    }

    public async Task<User> GetByLoginAsync(string login)
    {
        User? user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Login == login);

        if (user is null)
            throw new KeyNotFoundException();

        return user;
    }

    public async Task UpdateByIdAsync(
        Guid id,
        string login,
        string lastName,
        string firstName)
    {
        bool exists = await _context.Users
            .AnyAsync(u => u.Login == login && u.Id != id);

        if (exists)
            throw new InvalidOperationException();

        int result = await _context.Users
            .Where(u => u.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.Login, login)
                .SetProperty(u => u.LastName, lastName)
                .SetProperty(u => u.FirstName, firstName));

        if (result == 0)
            throw new KeyNotFoundException();
    }

    public async Task RemoveByIdAsync(Guid id)
    {
        int result = await _context.Users
            .Where(u => u.Id == id)
            .ExecuteDeleteAsync();

        if (result == 0)
            throw new KeyNotFoundException();
    }
}