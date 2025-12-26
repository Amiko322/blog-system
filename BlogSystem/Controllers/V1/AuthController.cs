using Asp.Versioning;
using BlogSystem.Contracts.Users;
using BlogSystem.Models;
using BlogSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogSystem.Controllers.V1;

[AllowAnonymous]
[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[ApiVersion(1)]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(
        IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Login) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrEmpty(request.LastName) ||
            string.IsNullOrEmpty(request.FirstName))
        {
            return BadRequest(new
            {
                Message = "Invalid request data.",
            });
        }

        User user = await _userService.AddAsync(
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
        };

        return Ok(userDto);
    }
}
