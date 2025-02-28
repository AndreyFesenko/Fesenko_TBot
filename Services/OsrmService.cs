using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Fesenko_TBot.Services;
public class OsrmService
{
    private readonly HttpClient _httpClient;

    public OsrmService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<double> GetDistanceAsync(string start, string end)
    {
        start = start.Replace(" ", "");
        end = end.Replace(" ", "");
        // Меняем широту и долготу местами
        var startCoords = start.Split(',');
        var endCoords = end.Split(',');

        if (startCoords.Length != 2 || endCoords.Length != 2)
        {
            throw new Exception("Неверный формат координат.");
        }

        var startLonLat = $"{startCoords[1]},{startCoords[0]}"; // Меняем местами
        var endLonLat = $"{endCoords[1]},{endCoords[0]}"; // Меняем местами

        var url = $"http://router.project-osrm.org/route/v1/driving/{startLonLat};{endLonLat}?overview=false";
        var response = await _httpClient.GetAsync(url);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);
            var distance = json.RootElement.GetProperty("routes")[0].GetProperty("distance").GetDouble();
            return distance / 1000; // Переводим метры в километры
        }

        throw new Exception("Не удалось получить расстояние от OSRM API.");
    }
}
