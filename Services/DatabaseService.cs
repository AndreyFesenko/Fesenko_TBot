using Fesenko_TBot.Interfaces;
using Fesenko_TBot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;


namespace Fesenko_TBot.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly OctopusDbContext _dbContext;

        public async Task<Models.User> GetUserByLoginAsync(string login)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.Login == login);
        }
        public DatabaseService(OctopusDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<string>> GetCitiesAsync()
        {
            return await _dbContext.Incident
                .Select(i => i.City)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<Incident>> GetIncidentsByCityAsync(string cityName)
        {
            return await _dbContext.Incident
                .Where(i => i.City == cityName && i.Status == "Open")
                .OrderBy(i => i.Deadline)
                .ToListAsync();
        }

        public async Task<List<Engineer>> GetEngineersByCityAsync(string cityName)
        {
            return await _dbContext.Engineer
                .Where(e => e.Status == "Free" && e.City == cityName)
                .ToListAsync();
        }

        public async Task<Incident> GetIncidentByIdAsync(int incidentId)
        {
            return await _dbContext.Incident.FirstOrDefaultAsync(i => i.IdInc == incidentId);
        }

        public async Task<Engineer> GetEngineerByIdAsync(int engineerId)
        {
            return await _dbContext.Engineer.FirstOrDefaultAsync(e => e.IdEng == engineerId);
        }

        public async Task AssignEngineerToIncidentAsync(int engineerId, int incidentId)
        {
            var incident = await _dbContext.Incident.FirstOrDefaultAsync(i => i.IdInc == incidentId);
            var engineer = await _dbContext.Engineer.FirstOrDefaultAsync(e => e.IdEng == engineerId);

            if (incident != null && engineer != null)
            {
                incident.IdEng = engineerId;
                incident.Status = "assigned";
                engineer.Status = "assigned";
                await _dbContext.SaveChangesAsync();
            }
        }
    }

}
