namespace BlogSystem.RabbitMq.Models;

public class StandardRequestMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Version { get; set; } = "v1";

    public string Action { get; set; } = default!;

    public object? Data { get; set; }

    public string? Auth { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
