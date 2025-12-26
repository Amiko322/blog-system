namespace BlogSystem.RabbitMq.Models;

public class StandardResponseMessage
{
    public Guid CorrelationId { get; set; }

    public string? Status { get; set; }

    public object? Data { get; set; }

    public string? Error { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
