
namespace Fesenko_TBot.Interfaces;
public interface IRedisService
{
    Task<T> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
    Task<double> GetDistanceAsync(string startCoordinates, string endCoordinates);

    Task SetDistanceAsync(string startCoordinates, string endCoordinates, double distance, TimeSpan? expiry = null);
}

