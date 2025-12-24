using System.ComponentModel.DataAnnotations.Schema;

namespace BlogSystem.Models;

public class Post
{
    public Guid Id { get; set; }

    public string Title { get; set; } = default!;

    public string Content { get; set; } = default!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(User))]
    public Guid UserId { get; set; }

    public User User { get; set; } = default!;
}