namespace BlogSystem.Services;

public interface IPasswordHasher
{
    string Generate(string password);

    bool Verify(string password, string passwordHash);
}

public class PasswordHasher : IPasswordHasher
{
    public string Generate(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string passwordHash) =>
        BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
