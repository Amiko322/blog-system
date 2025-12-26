using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using System.Net;

namespace BlogSystem.Controllers.Internal;

[ApiController]
[Route("internal/redis")]
[ApiExplorerSettings(IgnoreApi = true)]
public class RedisController : ControllerBase
{
    private readonly IConnectionMultiplexer _redis;

    public RedisController(
        IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    [HttpGet("keys")]
    public IActionResult GetKeys()
    {
        EndPoint endpoint = _redis.GetEndPoints().First();

        IServer server = _redis.GetServer(endpoint);

        IEnumerable<string> keys = server
            .Keys()
            .Select(k => k.ToString())
            .ToList();

        return Ok(new
        {
            Size = server.DatabaseSize(),
            Keys = keys,
        });
    }
}
