using Fesenko_TBot.Models;

namespace Fesenko_TBot.Interfaces
{
    public interface IDatabaseService
    {
        Task<List<string>> GetCitiesAsync();
        Task<List<Incident>> GetIncidentsByCityAsync(string cityName);
        Task<List<Engineer>> GetEngineersByCityAsync(string cityName);
        Task<Incident> GetIncidentByIdAsync(int incidentId);
        Task<Engineer> GetEngineerByIdAsync(int engineerId);
        Task AssignEngineerToIncidentAsync(int engineerId, int incidentId);
        Task<Models.User> GetUserByLoginAsync(string login);
    }
}
