using System.Security.Cryptography;
using Fesenko_TBot.Interfaces;
using Fesenko_TBot.Models;

namespace Fesenko_TBot.Services
{
    public class AuthService
    {
        private readonly IDatabaseService _databaseService;
        private readonly IRedisService _redisService;

        public AuthService(IDatabaseService databaseService, IRedisService redisService)
        {
            _databaseService = databaseService;
            _redisService = redisService;
        }

        // Метод для проверки авторизации
        public async Task<bool> AuthenticateUserAsync(string login, string password)
        {
            // Проверяем кэш Redis
            var cacheKey = $"user_{login}";
            var cachedUser = await _redisService.GetAsync<User>(cacheKey);

            User user;
            if (cachedUser != null)
            {
                user = cachedUser;
            }
            else
            {
                // Если данных нет в кэше, запрашиваем из базы данных
                user = await _databaseService.GetUserByLoginAsync(login);
                if (user != null)
                {
                    // Сохраняем данные пользователя в Redis на 30 минут
                    await _redisService.SetAsync(cacheKey, user, TimeSpan.FromMinutes(30));
                }
            }

            if (user != null && VerifyPassword(password, user.PasswordHash))
            {
                return true;
            }

            return false;
        }

        // Метод для проверки пароля
        public bool VerifyPassword(string password, string storedHash)
        {
            byte[] storedBytes = Convert.FromBase64String(storedHash);
            byte[] salt = storedBytes.Take(16).ToArray();
            byte[] storedHashBytes = storedBytes.Skip(16).ToArray();

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(32);
                return hash.SequenceEqual(storedHashBytes);
            }
        }
    }
}
