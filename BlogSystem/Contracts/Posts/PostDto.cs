namespace BlogSystem.Contracts.Posts;

public sealed class PostDto
{
    public Guid Id { get; set; }

    public string Title { get; set; } = default!;

    public string Content { get; set; } = default!;

    public DateTime CreatedAt { get; set; }

    public Guid UserId { get; set; }
}
