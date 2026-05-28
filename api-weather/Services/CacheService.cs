using StackExchange.Redis;

namespace WeatherApi.Services;

public class CacheService(IConnectionMultiplexer redis)
{
    private readonly IDatabase _db = redis.GetDatabase();

    public async Task<string?> GetAsync(string key)
    {
        var val = await _db.StringGetAsync(key);
        return val.HasValue ? (string?)val : null;
    }

    public async Task SetAsync(string key, string value, TimeSpan ttl) =>
        await _db.StringSetAsync(key, value, ttl);
}
