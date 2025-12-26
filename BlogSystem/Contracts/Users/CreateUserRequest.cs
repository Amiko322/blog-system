namespace BlogSystem.Contracts.Users;

public sealed class CreateUserRequest
{
    public string Login { get; set; } = default!;

    public string Password { get; set; } = default!;

    public string LastName { get; set; } = default!;

    public string FirstName { get; set; } = default!;
}
