using Asp.Versioning;
using BlogSystem.Configuration.Options;
using BlogSystem.Contracts;
using BlogSystem.Contracts.Posts;
using BlogSystem.Contracts.Users;
using BlogSystem.RabbitMq.Models;
using BlogSystem.RabbitMq.Producers;
using BlogSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BlogSystem.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/rabbit")]
[Produces("application/json")]
[ApiVersion(2)]
public class RabbitMqController : ControllerBase
{
    private readonly IRabbitMqProducer _producer;
    private readonly IPasswordHasher _hasher;
    private readonly TokenOptions _options;

    public RabbitMqController(
        IRabbitMqProducer producer,
        IPasswordHasher hasher,
        IOptions<TokenOptions> options)
    {
        _producer = producer;
        _hasher = hasher;
        _options = options.Value;
    }

    [HttpPost("users")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CreatUser([FromBody] CreateUserRequest request)
    {
        StandardRequestMessage message = new()
        {
            // Guid.Parse("11111111-1111-1111-1111-111111111111")
            Id = Guid.NewGuid(),
            Version = "v1",
            Action = "create_user",
            Auth = _options.Key,
            Data = new
            {
                request.Login,
                PasswordHash = _hasher.Generate(request.Password),
                request.LastName,
                request.FirstName,
            },
        };

        StandardResponseMessage response = await _producer.SendAsync(message);

        return response.Status == "Ok"
            ? Ok(response.Data)
            : BadRequest(new
            {
                response.Error,
            });
    }

    [HttpGet("users/{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        StandardRequestMessage message = new()
        {
            Id = Guid.NewGuid(),
            Version = "v1",
            Action = "get_user",
            Auth = _options.Key,
            Data = new
            {
                UserId = id.ToString(),
            },
        };

        StandardResponseMessage response = await _producer.SendAsync(message);

        return response.Status == "Ok"
            ? Ok(response.Data)
            : BadRequest(new
            {
                response.Error,
            });
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        StandardRequestMessage message = new()
        {
            Id = Guid.NewGuid(),
            Version = "v1",
            Action = "get_users",
            Auth = _options.Key,
            Data = new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
            },
        };

        StandardResponseMessage response = await _producer.SendAsync(message);

        return response.Status == "Ok" ?
            Ok(response.Data) :
            BadRequest(new
            {
                response.Error,
            });
    }

    [HttpPut("users/{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> UpdateUserById(
        Guid id,
        [FromBody] UpdateUserRequest request)
    {
        StandardRequestMessage message = new()
        {
            Id = Guid.NewGuid(),
            Version = "v1",
            Action = "update_user",
            Auth = _options.Key,
            Data = new
            {
                UserId = id.ToString(),
                request.Login,
                request.LastName,
                request.FirstName,
            },
        };

        StandardResponseMessage response = await _producer.SendAsync(message);

        return response.Status == "Ok" ?
            Ok(response.Data) :
            BadRequest(new
            {
                response.Error,
            });
    }

    [HttpDelete("users/{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> DeleteUserById(Guid id)
    {
        StandardRequestMessage message = new()
        {
            Id = Guid.NewGuid(),
            Version = "v1",
            Action = "delete_user",
            Auth = _options.Key,
            Data = new
            {
                UserId = id.ToString(),
            },
        };

        StandardResponseMessage response = await _producer.SendAsync(message);

        return response.Status == "Ok" ?
            Ok(response.Data) :
            BadRequest(new
            {
                response.Error,
            });
    }

    [HttpPost("posts")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request)
    {
        StandardRequestMessage message = new()
        {
            Id = Guid.NewGuid(),
            Version = "v1",
            Action = "create_post",
            Auth = _options.Key,
            Data = new
            {
                request.Title,
                request.Content,
                UserId = request.UserId.ToString(),
            },
        };

        StandardResponseMessage response = await _producer.SendAsync(message);

        return response.Status == "Ok" ?
            Ok(response.Data) :
            BadRequest(new
            {
                response.Error,
            });
    }

    [HttpGet("posts/{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetPostById(Guid id)
    {
        StandardRequestMessage message = new()
        {
            Id = Guid.NewGuid(),
            Version = "v1",
            Action = "get_post",
            Auth = _options.Key,
            Data = new
            {
                PostId = id.ToString(),
            },
        };

        StandardResponseMessage response = await _producer.SendAsync(message);

        return response.Status == "Ok" ?
            Ok(response.Data) :
            BadRequest(new
            {
                response.Error,
            });
    }

    [HttpGet("posts")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetPosts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        StandardRequestMessage message = new()
        {
            Id = Guid.NewGuid(),
            Version = "v1",
            Action = "get_posts",
            Auth = _options.Key,
            Data = new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
            },
        };

        StandardResponseMessage response = await _producer.SendAsync(message);

        return response.Status == "Ok" ?
            Ok(response.Data) :
            BadRequest(new
            {
                response.Error,
            });
    }

    [HttpPut("posts/{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> UpdatePostById(
        Guid id,
        [FromBody] UpdatePostRequest request)
    {
        StandardRequestMessage message = new()
        {
            Id = Guid.NewGuid(),
            Version = "v1",
            Action = "update_post",
            Auth = _options.Key,
            Data = new
            {
                PostId = id.ToString(),
                request.Title,
                request.Content,
            },
        };

        StandardResponseMessage response = await _producer.SendAsync(message);

        return response.Status == "Ok" ?
            Ok(response.Data) :
            BadRequest(new
            {
                response.Error,
            });
    }

    [HttpDelete("posts/{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> DeletePostById(Guid id)
    {
        StandardRequestMessage message = new()
        {
            Id = Guid.NewGuid(),
            Version = "v1",
            Action = "delete_post",
            Auth = _options.Key,
            Data = new
            {
                PostId = id.ToString(),
            },
        };

        StandardResponseMessage response = await _producer.SendAsync(message);

        return response.Status == "Ok" ?
            Ok(response.Data) :
            BadRequest(new
            {
                response.Error,
            });
    }
}