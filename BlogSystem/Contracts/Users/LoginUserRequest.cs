namespace BlogSystem.Contracts.Users;

public class LoginUserRequest
{
    public string Login { get; set; } = default!;

    public string Password { get; set; } = default!;
}
