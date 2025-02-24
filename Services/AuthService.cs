using Fesenko_TBot.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace Fesenko_TBot.Services
{
    public class AuthService
    {
        private readonly IDatabaseService _databaseService;

        public AuthService(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        // Метод для хэширования пароля
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }

        // Метод для проверки авторизации
        public async Task<bool> AuthenticateUserAsync(string login, string password)
        {
            var hashedPassword = HashPassword(password);
            var user = await _databaseService.GetUserByLoginAsync(login);

            if (user != null && user.PasswordHash == hashedPassword)
            {
                return true;
            }

            return false;
        }

    }
}