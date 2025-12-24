using Asp.Versioning;
using BlogSystem.Contracts.Posts;
using BlogSystem.Models;
using BlogSystem.Services;
using BlogSystem.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogSystem.Controllers.V1;

[AllowAnonymous]
[ApiController]
[Route("api/v{version:apiVersion}/posts")]
[ApiVersion(1)]
public class PostController : ControllerBase
{
    private readonly IPostService _postService;

    public PostController(
        IPostService postService)
    {
        _postService = postService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreatePostRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new
            {
                Message = "Invalid request data.",
            });
        }

        Post post = await _postService.AddAsync(
            request.Title,
            request.Content,
            request.UserId);

        PostDto postDto = new()
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            CreatedAt = post.CreatedAt,
            UserId = post.UserId,
        };

        return Ok(postDto);
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize)
    {
        PaginationValidator validator = new(pageNumber, pageSize);

        IEnumerable<Post> posts = await _postService
            .GetAsync(validator.PageNumber, validator.PageSize);

        IEnumerable<PostDto> postsDto = posts
            .Select(p => new PostDto
            {
                Id = p.Id,
                Title = p.Title,
                Content = p.Content,
                CreatedAt = p.CreatedAt,
                UserId = p.UserId,
            })
            .ToList();

        return Ok(postsDto);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid id)
    {
        Post post = await _postService.GetByIdAsync(id);

        PostDto postDto = new()
        {
            Id = post.Id,
            Title = post.Title,
            Content = post.Content,
            CreatedAt = post.CreatedAt,
            UserId = post.UserId,
        };

        return Ok(postDto);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateByIdAsync(
        Guid id,
        [FromBody] UpdatePostRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new
            {
                Message = "Invalid request data.",
            });
        }

        await _postService.UpdateByIdAsync(
            id,
            request.Title,
            request.Content);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteByIdAsync(Guid id)
    {
        await _postService.RemoveByIdAsync(id);

        return NoContent();
    }
}
