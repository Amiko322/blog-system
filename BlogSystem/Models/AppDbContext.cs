using Microsoft.EntityFrameworkCore;

namespace BlogSystem.Models;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }

    public DbSet<Post> Posts { get; set; }

    public AppDbContext(DbContextOptions options)
        : base(options)
    {
    }
}
