using Asp.Versioning;
using BlogSystem.Contracts;
using BlogSystem.Contracts.Posts;
using BlogSystem.Models;
using BlogSystem.Services;
using BlogSystem.Validators;
using IdempotentAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Dynamic;

namespace BlogSystem.Controllers.V2;

[Authorize]
[ApiController]
[Route("api/v{version:apiVersion}/posts")]
[ApiVersion(2)]
public class PostController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly IDataShaper<PostDto> _dataShaper;

    public PostController(
        IPostService postService,
        IDataShaper<PostDto> dataShaper)
    {
        _postService = postService;
        _dataShaper = dataShaper;
    }

    [Idempotent]
    [HttpPost]
    [ProducesResponseType(typeof(PostDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CreateAsync([FromBody] CreatePostRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new ExceptionResponse
            {
                StatusCode = StatusCodes.Status400BadRequest,
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
    [ProducesResponseType(typeof(IEnumerable<PostDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<ExpandoObject>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetAsync(
        [FromQuery] string? include,
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


        if (string.IsNullOrWhiteSpace(include))
            return Ok(postsDto);

        IEnumerable<ExpandoObject> shapedObjects = _dataShaper.ShapeData(postsDto, include);

        return Ok(shapedObjects);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PostDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExpandoObject), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetByIdAsync(
        Guid id,
        [FromQuery] string? fields)
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

        if (string.IsNullOrWhiteSpace(fields))
            return Ok(postDto);

        ExpandoObject shapedObject = _dataShaper.ShapeData(postDto, fields);

        return Ok(shapedObject);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> UpdateByIdAsync(
        Guid id,
        [FromBody] UpdatePostRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new ExceptionResponse
            {
                StatusCode = StatusCodes.Status400BadRequest,
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> DeleteByIdAsync(Guid id)
    {
        await _postService.RemoveByIdAsync(id);

        return NoContent();
    }
}
