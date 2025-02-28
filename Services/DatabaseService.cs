using Fesenko_TBot.Interfaces;
using Fesenko_TBot.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Fesenko_TBot.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly OctopusDbContext _dbContext;
        private readonly IRedisService _redisService;

        public DatabaseService(OctopusDbContext dbContext, IRedisService redisService)
        {
            _dbContext = dbContext;
            _redisService = redisService;
        }

        public async Task<ATM> GetATMByIdAsync(string atmId)
        {
            var cacheKey = $"atm_{atmId}";
            var cachedATM = await _redisService.GetAsync<ATM>(cacheKey);

            if (cachedATM != null)
            {
                return cachedATM;
            }

            var atm = await _dbContext.ATM
                .FirstOrDefaultAsync(a => a.IdATM == atmId);

            if (atm != null)
            {
                await _redisService.SetAsync(cacheKey, atm, TimeSpan.FromMinutes(5));
            }

            return atm;
        }

        public async Task<List<string>> GetCitiesAsync()
        {
            var cacheKey = "cities";
            var cachedCities = await _redisService.GetAsync<List<string>>(cacheKey);

            if (cachedCities != null)
            {
                return cachedCities;
            }

            var cities = await _dbContext.Incident
                .Select(i => i.City)
                .Distinct()
                .ToListAsync();

            await _redisService.SetAsync(cacheKey, cities, TimeSpan.FromMinutes(10));

            return cities;
        }

        public async Task<List<Incident>> GetIncidentsByCityAsync(string cityName)
        {
            var cacheKey = $"incidents_{cityName}";
            var cachedIncidents = await _redisService.GetAsync<List<Incident>>(cacheKey);

            if (cachedIncidents != null)
            {
                return cachedIncidents;
            }

            var incidents = await _dbContext.Incident
                .Where(i => i.City == cityName && i.Status == "Open")
                .OrderBy(i => i.Deadline)
                .ToListAsync();

            await _redisService.SetAsync(cacheKey, incidents, TimeSpan.FromMinutes(5));

            return incidents;
        }

        public async Task<List<Engineer>> GetEngineersByCityAsync(string cityName)
        {
            var cacheKey = $"engineers_{cityName}";
            var cachedEngineers = await _redisService.GetAsync<List<Engineer>>(cacheKey);

            if (cachedEngineers != null)
            {
                return cachedEngineers;
            }

            var engineers = await _dbContext.Engineer
                .Where(e => e.Status == "Free" && e.City == cityName)
                .ToListAsync();

            await _redisService.SetAsync(cacheKey, engineers, TimeSpan.FromMinutes(5));

            return engineers;
        }

        public async Task<Incident> GetIncidentByIdAsync(int incidentId)
        {
            var cacheKey = $"incident_{incidentId}";
            var cachedIncident = await _redisService.GetAsync<Incident>(cacheKey);

            if (cachedIncident != null)
            {
                return cachedIncident;
            }

            var incident = await _dbContext.Incident
                .FirstOrDefaultAsync(i => i.IdInc == incidentId);

            if (incident != null)
            {
                await _redisService.SetAsync(cacheKey, incident, TimeSpan.FromMinutes(5));
            }

            return incident;
        }

        public async Task<Engineer> GetEngineerByIdAsync(int engineerId)
        {
            var cacheKey = $"engineer_{engineerId}";
            var cachedEngineer = await _redisService.GetAsync<Engineer>(cacheKey);

            if (cachedEngineer != null)
            {
                return cachedEngineer;
            }

            var engineer = await _dbContext.Engineer
                .FirstOrDefaultAsync(e => e.IdEng == engineerId);

            if (engineer != null)
            {
                await _redisService.SetAsync(cacheKey, engineer, TimeSpan.FromMinutes(5));
            }

            return engineer;
        }

        public async Task AssignEngineerToIncidentAsync(int engineerId, int incidentId)
        {
            try
            {
                var incident = await _dbContext.Incident
                    .FirstOrDefaultAsync(i => i.IdInc == incidentId);
                var engineer = await _dbContext.Engineer
                    .FirstOrDefaultAsync(e => e.IdEng == engineerId);

                if (incident == null || engineer == null)
                {
                    Log.Logger.Warning($"Инцидент {incidentId} или инженер {engineerId} не найдены.");
                    return;
                }

                incident.IdEng = engineerId;
                incident.Status = "assigned";
                engineer.Status = "assigned";

                await _dbContext.SaveChangesAsync();

                // Очищаем кэш для инцидента и инженера
                await _redisService.RemoveAsync($"incident_{incidentId}");
                await _redisService.RemoveAsync($"engineer_{engineerId}");
                await _redisService.RemoveAsync($"incidents_{incident.City}");
                await _redisService.RemoveAsync($"engineers_{engineer.City}");

                Log.Logger.Information($"Инженер {engineer.NameEng} назначен на инцидент {incidentId}.");
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"Ошибка при назначении инженера на инцидент: {ex.Message}");
                throw;
            }
        }

        public async ValueTask<User> GetUserByLoginAsync(string login)
        {
            var cacheKey = $"user_{login}";
            var cachedUser = await _redisService.GetAsync<User>(cacheKey);

            if (cachedUser != null)
            {
                return cachedUser;
            }

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Login == login);

            if (user != null)
            {
                await _redisService.SetAsync(cacheKey, user, TimeSpan.FromMinutes(30));
            }

            return user;
        }
    }

}

