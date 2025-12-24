using Asp.Versioning;
using BlogSystem.Contracts.Users;
using BlogSystem.Models;
using BlogSystem.Services;
using BlogSystem.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlogSystem.Controllers.V1;

[AllowAnonymous]
[ApiController]
[Route("api/v{version:apiVersion}/users")]
[ApiVersion(1)]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(
        IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize)
    {
        PaginationValidator validator = new(pageNumber, pageSize);

        IEnumerable<User> users = await _userService
            .GetAsync(validator.PageNumber, validator.PageSize);

        IEnumerable<UserDto> usersDto = users
            .Select(u => new UserDto
            {
                Id = u.Id,
                Login = u.Login,
                LastName = u.LastName,
                FirstName = u.FirstName,
            })
            .ToList();

        return Ok(usersDto);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid id)
    {
        User user = await _userService.GetByIdAsync(id);

        UserDto userDto = new()
        {
            Id = user.Id,
            Login = user.Login,
            LastName = user.LastName,
            FirstName = user.FirstName,
        };

        return Ok(userDto);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateByIdAsync(
        Guid id,
        [FromBody] UpdateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Login) ||
            string.IsNullOrEmpty(request.LastName) ||
            string.IsNullOrEmpty(request.FirstName))
        {
            return BadRequest(new
            {
                Message = "Invalid request data.",
            });
        }

        await _userService.UpdateByIdAsync(
            id,
            request.Login,
            request.LastName,
            request.FirstName);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteByIdAsync(Guid id)
    {
        await _userService.RemoveByIdAsync(id);

        return NoContent();
    }
}
