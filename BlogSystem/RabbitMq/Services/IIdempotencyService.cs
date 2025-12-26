using System.Collections.Concurrent;

namespace BlogSystem.RabbitMq.Services;

public interface IIdempotencyService
{
    Task<bool> IsProcessedAsync(Guid messageId);

    Task MarkAsProcessedAsync(Guid messageId);
}

public class IdempotencyService : IIdempotencyService
{
    private readonly ConcurrentDictionary<Guid, bool> _messages = new();

    public Task<bool> IsProcessedAsync(Guid messageId)
    {
        return Task.FromResult(_messages.ContainsKey(messageId));
    }

    public Task MarkAsProcessedAsync(Guid messageId)
    {
        _messages.TryAdd(messageId, true);
        return Task.CompletedTask;
    }
}
