namespace BlogSystem.Contracts.Posts;

public sealed class UpdatePostRequest
{
    public string Title { get; set; } = default!;

    public string Content { get; set; } = default!;
}
