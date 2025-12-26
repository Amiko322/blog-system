namespace BlogSystem.Configuration.Options;

public class RabbitMqOptions
{
    public string HostName { get; set; } = default!;

    public int Port { get; set; }

    public string UserName { get; set; } = default!;

    public string Password { get; set; } = default!;

    public string VirtualHost { get; set; } = default!;
}
