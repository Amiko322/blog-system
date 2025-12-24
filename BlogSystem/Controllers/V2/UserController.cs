using Asp.Versioning;
using BlogSystem.Contracts;
using BlogSystem.Contracts.Posts;
using BlogSystem.Contracts.Users;
using BlogSystem.Models;
using BlogSystem.Services;
using BlogSystem.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Dynamic;

namespace BlogSystem.Controllers.V2;

[Authorize]
[ApiController]
[Route("api/v{version:apiVersion}/users")]
[ApiVersion(2)]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IDataShaper<UserDto> _dataShaper;

    public UserController(
        IUserService userService,
        IDataShaper<UserDto> dataShaper)
    {
        _userService = userService;
        _dataShaper = dataShaper;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<ExpandoObject>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetAsync(
        [FromQuery] string? include,
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
                RegisteredAt = u.RegisteredAt,
            })
            .ToList();

        if (string.IsNullOrWhiteSpace(include))
            return Ok(usersDto);

        IEnumerable<ExpandoObject> shapedObjects = _dataShaper.ShapeData(usersDto, include);

        return Ok(shapedObjects);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExpandoObject), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetByIdAsync(
        Guid id,
        [FromQuery] string? fields)
    {
        User user = await _userService.GetByIdAsync(id);

        UserDto userDto = new()
        {
            Id = user.Id,
            Login = user.Login,
            LastName = user.LastName,
            FirstName = user.FirstName,
            RegisteredAt = user.RegisteredAt,
        };

        if (string.IsNullOrWhiteSpace(fields))
            return Ok(userDto);

        ExpandoObject shapedObject = _dataShaper.ShapeData(userDto, fields);

        return Ok(shapedObject);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> UpdateByIdAsync(
        Guid id,
        [FromBody] UpdateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Login) ||
            string.IsNullOrEmpty(request.LastName) ||
            string.IsNullOrEmpty(request.FirstName))
        {
            return BadRequest(new ExceptionResponse
            {
                StatusCode = StatusCodes.Status400BadRequest,
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> DeleteByIdAsync(Guid id)
    {
        await _userService.RemoveByIdAsync(id);

        return NoContent();
    }
}
