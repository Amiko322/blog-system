namespace BlogSystem.Contracts.Users;

public sealed class UserDto
{
    public Guid Id { get; set; }

    public string Login { get; set; } = default!;

    public string LastName { get; set; } = default!;

    public string FirstName { get; set; } = default!;

    public DateTime? RegisteredAt { get; set; }
}
