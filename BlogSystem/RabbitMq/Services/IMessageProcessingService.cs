using BlogSystem.Configuration.Constants;
using BlogSystem.Contracts.Posts;
using BlogSystem.Contracts.Users;
using BlogSystem.Models;
using BlogSystem.RabbitMq.Models;
using BlogSystem.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace BlogSystem.RabbitMq.Services;

public interface IMessageProcessingService
{
    Task ProcessMessageAsync(
        BasicDeliverEventArgs ea,
        IServiceProvider sp,
        IModel channel);
}

public class MessageProcessingService : IMessageProcessingService
{
    private const string API_KEY = "KM]>Q^![e|9lUT6&G)x?!x%cE^}[z)UpuSmV)Z_T!Pf4;@n!@:I1h,/GSj#Vl}";

    private readonly ILogger<MessageProcessingService> _logger;

    public MessageProcessingService(
        ILogger<MessageProcessingService> logger)
    {
        _logger = logger;
    }

    public async Task ProcessMessageAsync(
        BasicDeliverEventArgs ea, 
        IServiceProvider sp, 
        IModel channel)
    {
        string json = Encoding.UTF8.GetString(ea.Body.ToArray());

        try
        {
            using IServiceScope scope = sp.CreateScope();

            IUserService userService = scope.ServiceProvider
                .GetRequiredService<IUserService>();
            IPostService postService = scope.ServiceProvider
                .GetRequiredService<IPostService>();
            IIdempotencyService idempotencyService = scope.ServiceProvider
                .GetRequiredService<IIdempotencyService>();

            StandardRequestMessage? request = JsonSerializer.Deserialize<StandardRequestMessage>(json);

            if (request is null)
            {
                _logger.LogError(
                    "Bad request (cannot deserialize): {Json}", 
                    json);

                channel.BasicNack(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false,
                    requeue: false);

                return;
            }

            if (idempotencyService is not null && await idempotencyService.IsProcessedAsync(request.Id))
            {
                StandardResponseMessage cached = new()
                {
                    CorrelationId = request.Id,
                    Status = "Ok",
                    Data = new 
                    { 
                        Message = "Already processed", 
                        IsCached = true,
                    },
                    Error = null,
                };

                await SendResponseAsync(cached, ea, channel);

                channel.BasicAck(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false);

                return;
            }

            if (!IsAuthorized(request.Auth))
            {
                StandardResponseMessage unauthorized = new()
                {
                    CorrelationId = request.Id,
                    Status = "Error",
                    Data = null,
                    Error = "Unauthorized",
                };

                await SendResponseAsync(unauthorized, ea, channel);

                channel.BasicAck(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false);

                return;
            }

            (bool Ok, object? Data, string? Error) action = await HandleAction(
                request,
                userService,
                postService);

            if (idempotencyService is not null && action.Ok)
            {
                await idempotencyService.MarkAsProcessedAsync(request.Id);
            }

            StandardResponseMessage response = new()
            {
                CorrelationId = request.Id,
                Status = action.Ok ? "Ok" : "Error",
                Data = action.Data,
                Error = action.Ok ? null : action.Error,
            };

            await SendResponseAsync(response, ea, channel);

            channel.BasicAck(
                deliveryTag: ea.DeliveryTag,
                multiple: false);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON: {Json}", json);
            
            SendToDlq(channel, json, ex.Message);

            channel.BasicNack(
                deliveryTag: ea.DeliveryTag,
                multiple: false,
                requeue: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");

            var retryCount = GetRetryCountFromHeader(ea);

            if (retryCount >= RabbitMqTopology.MAX_RETRY_COUNT)
            {
                var correlation = ea.BasicProperties?.CorrelationId;
                Guid correlationId = Guid.TryParse(correlation, out Guid guid) ? guid : Guid.Empty;

                var response = new StandardResponseMessage
                {
                    CorrelationId = correlationId,
                    Status = "Error",
                    Data = null,
                    Error = "Failed after retries. Moved to DLQ",
                };

                await SendResponseAsync(response, ea, channel);

                SendToDlq(channel, json, ex.Message);

                channel.BasicAck(ea.DeliveryTag, multiple: false);
                return;
            }

            await Task.Delay(CalculateDelay(retryCount));

            var props = channel.CreateBasicProperties();
            props.Persistent = true;
            props.CorrelationId = ea.BasicProperties?.CorrelationId;
            props.ReplyTo = ea.BasicProperties?.ReplyTo;

            props.Headers = ea.BasicProperties?.Headers ?? new Dictionary<string, object>();
            props.Headers["x-retry-count"] = (retryCount + 1).ToString();

            channel.BasicPublish(
                exchange: string.Empty,
                routingKey: "api.requests",
                basicProperties: props,
                body: Encoding.UTF8.GetBytes(json));

            channel.BasicAck(ea.DeliveryTag, multiple: false);
        }
    }

    private static bool IsAuthorized(string? auth)
    {
        return !string.IsNullOrWhiteSpace(auth) && auth == API_KEY;
    }

    private static async Task<(bool ok, object? data, string? error)> HandleAction(
        StandardRequestMessage request,
        IUserService userService,
        IPostService postService)
    {
        if (request.Data is null)
            return default;

        JsonElement dataEl = JsonDocument.Parse(JsonSerializer.Serialize(request.Data)).RootElement;

        switch (request.Action)
        {
            case "create_user":
                {
                    //throw new Exception();

                    User user = await userService.AddAsync(
                        GetString(dataEl, "Login"),
                        GetString(dataEl, "PasswordHash"),
                        GetString(dataEl, "LastName"),
                        GetString(dataEl, "FirstName"));

                    return (true, new { UserId = user.Id }, null);
                }

            case "get_user":
                {
                    User user = await userService.GetByIdAsync(GetGuid(dataEl, "UserId"));

                    UserDto userDto = new()
                    {
                        Id = user.Id,
                        Login = user.Login,
                        LastName = user.LastName,
                        FirstName = user.FirstName,
                        RegisteredAt = user.RegisteredAt,
                    };

                    return (true, userDto, null);
                }

            case "get_users":
                {
                    IEnumerable<User> users = await userService.GetAsync(
                        GetInt(dataEl, "PageNumber", 1),
                        GetInt(dataEl, "PageSize", 10));

                    IEnumerable<UserDto> usersDto = users
                        .Select(u => new UserDto
                        {
                            Id = u.Id,
                            Login = u.Login,
                            LastName = u.LastName,
                            FirstName = u.FirstName,
                            RegisteredAt = u.RegisteredAt,
                        })
                        .ToList();

                    return (true, usersDto, null);
                }

            case "update_user":
                {
                    await userService.UpdateByIdAsync(
                        GetGuid(dataEl, "UserId"),
                        GetString(dataEl, "Login"),
                        GetString(dataEl, "LastName"),
                        GetString(dataEl, "FirstName"));

                    return (true, new { Success = true }, null);
                }

            case "delete_user":
                {
                    await userService.RemoveByIdAsync(GetGuid(dataEl, "UserId"));

                    return (true, new { Success = true }, null);
                }

            case "create_post":
                {
                    Post post = await postService.AddAsync(
                        GetString(dataEl, "Title"),
                        GetString(dataEl, "Content"),
                        GetGuid(dataEl, "UserId"));

                    return (true, new { PostId = post.Id }, null);
                }

            case "get_post":
                {
                    Post post = await postService.GetByIdAsync(
                        GetGuid(dataEl, "PostId"));

                    PostDto postDto = new()
                    {
                        Id = post.Id,
                        Title = post.Title,
                        Content = post.Content,
                        CreatedAt = post.CreatedAt,
                        UserId = post.UserId,
                    };

                    return (true, postDto, null);
                }

            case "get_posts":
                {
                    IEnumerable<Post> posts = await postService.GetAsync(
                        GetInt(dataEl, "PageNumber", 1),
                        GetInt(dataEl, "PageSize", 10));

                    IEnumerable<PostDto> postsDto = posts
                        .Select(p => new PostDto
                        {
                            Id = p.Id,
                            Title = p.Title,
                            Content = p.Content,
                            CreatedAt = p.CreatedAt,
                            UserId = p.UserId,
                        })
                        .ToList();

                    return (true, postsDto, null);
                }

            case "update_post":
                {
                    await postService.UpdateByIdAsync(
                        GetGuid(dataEl, "PostId"),
                        GetString(dataEl, "Title"),
                        GetString(dataEl, "Content"));

                    return (true, new { Success = true }, null);
                }

            case "delete_post":
                {
                    await postService.RemoveByIdAsync(
                        GetGuid(dataEl, "PostId"));

                    return (true, new { Success = true }, null);
                }

            default:
                return (false, null, $"Unknown action: {request.Action}");
        }
    }

    private static async Task SendResponseAsync(
        StandardResponseMessage response,
        BasicDeliverEventArgs ea,
        IModel channel)
    {
        byte[] body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));

        IBasicProperties props = channel.CreateBasicProperties();
        props.Persistent = true;
        props.CorrelationId = ea.BasicProperties.CorrelationId ??
            response.CorrelationId.ToString();

        var replyTo = ea.BasicProperties?.ReplyTo;
        var targetQueue = string.IsNullOrWhiteSpace(replyTo) 
            ? RabbitMqTopology.API_RESPONSE_QUEUE 
            : replyTo;

        channel.BasicPublish(
            exchange: string.Empty,
            routingKey: targetQueue,
            basicProperties: props,
            body: body);

        await Task.CompletedTask;
    }

    private static int GetRetryCountFromHeader(BasicDeliverEventArgs ea)
    {
        var headers = ea.BasicProperties?.Headers;

        if (headers == null) 
            return 0;

        if (!headers.TryGetValue("x-retry-count", out var raw)) 
            return 0;

        if (raw is byte[] bytes)
        {
            return int.TryParse(
                Encoding.UTF8.GetString(bytes), 
                out var n) 
                    ? n 
                    : 0;
        }

        if (raw is int i) 
            return i;

        return 0;
    }

    private static int CalculateDelay(int retryCount) =>
        RabbitMqTopology.BASE_DELAY * (int)Math.Pow(2, retryCount);

    private static void SendToDlq(IModel channel, string messageJson, string error)
    {
        IBasicProperties props = channel.CreateBasicProperties();

        props.Headers = new Dictionary<string, object>
        {
            { "original_error", error },
            { "timestamp", DateTime.UtcNow.ToString("O") },
        };

        channel.BasicPublish(
            exchange: RabbitMqTopology.DEAD_LETTER_EXCHANGE,
            routingKey: RabbitMqTopology.DEAD_LETTER_ROUTING_KEY,
            props,
            Encoding.UTF8.GetBytes(messageJson));
    }

    private static string GetString(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out JsonElement p))
            throw new Exception($"Missing field: {name}");

        return p.GetString() ?? string.Empty;
    }

    private static Guid GetGuid(JsonElement el, string name)
    {
        string s = GetString(el, name);

        return Guid.Parse(s);
    }

    private static int GetInt(JsonElement el, string name, int def)
    {
        if (!el.TryGetProperty(name, out JsonElement p))
            return def;

        if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out int v))
            return v;

        if (p.ValueKind == JsonValueKind.String && int.TryParse(p.GetString(), out int s))
            return s;

        return def;
    }
}
