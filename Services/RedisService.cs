using System.Text.Json;
using Fesenko_TBot.Interfaces;
using StackExchange.Redis;

namespace Fesenko_TBot.Services;
public class RedisService : IRedisService
{
    private readonly IDatabase _redisDb;

    public RedisService(IConnectionMultiplexer redis)
    {
        _redisDb = redis.GetDatabase();
    }

    public async Task<T> GetAsync<T>(string key)
    {
        var value = await _redisDb.StringGetAsync(key);
        if (value.IsNullOrEmpty)
        {
            return default;
        }
        return JsonSerializer.Deserialize<T>(value);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var serializedValue = JsonSerializer.Serialize(value);
        await _redisDb.StringSetAsync(key, serializedValue, expiry);
    }

    public async Task RemoveAsync(string key)
    {
        await _redisDb.KeyDeleteAsync(key);
    }
}