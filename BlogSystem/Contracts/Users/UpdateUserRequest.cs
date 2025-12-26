namespace BlogSystem.Contracts.Users;

public sealed class UpdateUserRequest
{
    public string Login { get; set; } = default!;

    public string LastName { get; set; } = default!;

    public string FirstName { get; set; } = default!;
}
