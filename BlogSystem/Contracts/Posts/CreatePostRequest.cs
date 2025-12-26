using BlogSystem.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace BlogSystem.Contracts.Posts;

public sealed class CreatePostRequest
{
    public string Title { get; set; } = default!;

    public string Content { get; set; } = default!;

    public Guid UserId { get; set; }
}
