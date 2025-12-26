namespace BlogSystem.Models;

public class User
{
    public Guid Id { get; set; }

    public string Login { get; set; } = default!;

    public string PasswordHash { get; set; } = default!;

    public string LastName { get; set; } = default!;

    public string FirstName { get; set; } = default!;

    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    public ICollection<Post> Posts { get; set; } = [];
}