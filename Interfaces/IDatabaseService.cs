using Fesenko_TBot.Models;

namespace Fesenko_TBot.Interfaces
{
    public interface IDatabaseService
    {
        Task<List<string>> GetCitiesAsync();
        Task<List<Incident>> GetIncidentsByCityAsync(string cityName);
        Task<List<Engineer>> GetEngineersByCityAsync(string cityName);
        Task<Incident> GetIncidentByIdAsync(int incidentId);
        Task<ATM> GetATMByIdAsync(string atmId);
        Task<Engineer> GetEngineerByIdAsync(int engineerId);
        Task AssignEngineerToIncidentAsync(int engineerId, int incidentId);
        ValueTask<Models.User> GetUserByLoginAsync(string login);
    }
}
