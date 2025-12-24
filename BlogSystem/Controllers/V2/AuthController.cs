using Asp.Versioning;
using BlogSystem.Contracts;
using BlogSystem.Contracts.Users;
using BlogSystem.Models;
using BlogSystem.Services;
using IdempotentAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogSystem.Controllers.V2;

[AllowAnonymous]
[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[Produces("application/json")]
[ApiVersion(2)]
public class AuthController : ControllerBase
{
    private readonly IIdentityService _identityService;

    public AuthController(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    [Idempotent]
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RegisterAsync([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Login) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrEmpty(request.LastName) ||
            string.IsNullOrEmpty(request.FirstName))
        {
            return BadRequest(new ExceptionResponse
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = "Invalid request data.",
            });
        }

        User user = await _identityService.RegisterAsync(
            request.Login,
            request.Password,
            request.LastName,
            request.FirstName);

        UserDto userDto = new()
        {
            Id = user.Id,
            Login = user.Login,
            LastName = user.LastName,
            FirstName = user.FirstName,
            RegisteredAt = user.RegisteredAt,
        };

        return Ok(userDto);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(JwtTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> LoginAsync([FromBody] LoginUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Login) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ExceptionResponse
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = "Invalid request data.",
            });
        }

        JwtTokenResponse response = new()
        {
            Access = await _identityService.LoginAsync(
                request.Login,
                request.Password),
        };

        return Ok(response);
    }
}
