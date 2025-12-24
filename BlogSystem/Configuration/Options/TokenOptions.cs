namespace BlogSystem.Configuration.Options;

public sealed class TokenOptions
{
    public string Issuer { get; set; } = default!;

    public string Audience { get; set; } = default!;

    public string Key { get; set; } = default!;

    public double AccessExpiresHours { get; set; }
}
