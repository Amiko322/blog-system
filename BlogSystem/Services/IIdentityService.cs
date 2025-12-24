using BlogSystem.Models;
using System.Security.Authentication;

namespace BlogSystem.Services;

public interface IIdentityService
{
    Task<User> RegisterAsync(
        string login,
        string password,
        string lastName,
        string firstName);

    Task<string> LoginAsync(
        string login,
        string password);
}

public class IdentityService : IIdentityService
{
    private readonly IPasswordHasher _hasher;
    private readonly IUserService _userService;
    private readonly ITokenFactory _tokenFactory;

    public IdentityService(
        IPasswordHasher hasher,
        IUserService userService,
        ITokenFactory tokenFactory)
    {
        _hasher = hasher;
        _userService = userService;
        _tokenFactory = tokenFactory;
    }

    public async Task<User> RegisterAsync(
        string login,
        string password, 
        string lastName, 
        string firstName)
    {
        string passwordHash = _hasher.Generate(password);

        User user = await _userService.AddAsync(
            login,
            passwordHash,
            lastName,
            firstName);

        return user;
    }

    public async Task<string> LoginAsync(
        string login, 
        string password)
    {
        User user = await _userService.GetByLoginAsync(login);

        bool result = _hasher.Verify(password, user.PasswordHash);

        if (!result)
            throw new AuthenticationException();

        return _tokenFactory.Create(user.Id);
    }
}
