using System.Text.Json;
using Fesenko_TBot.Interfaces;
using StackExchange.Redis;

namespace Fesenko_TBot.Services;
public class RedisService : IRedisService
{
    private readonly StackExchange.Redis.IDatabase _redisDb;

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

    // Реализация метода для получения расстояния
    public async Task<double> GetDistanceAsync(string startCoordinates, string endCoordinates)
    {
        var cacheKey = GetDistanceCacheKey(startCoordinates, endCoordinates);
        var value = await _redisDb.StringGetAsync(cacheKey);
        if (value.IsNullOrEmpty)
        {
            return default;
        }
        return (double)value;
    }

    // Реализация метода для сохранения расстояния
    public async Task SetDistanceAsync(string startCoordinates, string endCoordinates, double distance, TimeSpan? expiry = null)
    {
        var cacheKey = GetDistanceCacheKey(startCoordinates, endCoordinates);
        await _redisDb.StringSetAsync(cacheKey, distance, expiry);
    }

    // Вспомогательный метод для формирования ключа кэша
    private string GetDistanceCacheKey(string startCoordinates, string endCoordinates)
    {
        return $"distance_{startCoordinates}_{endCoordinates}";
    }
}