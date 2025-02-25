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
        //private string HashPassword(string password)
        //{
        //    using (var sha256 = SHA256.Create())
        //    {
        //        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        //        return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
        //    }
        //}

        private string HashPassword(string password)
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(32);
                return Convert.ToBase64String(salt.Concat(hash).ToArray());
            }
        }

        // Метод для проверки авторизации
        public async Task<bool> AuthenticateUserAsync(string login, string password)
        {
            var hashedPassword = HashPassword(password);
            var user = await _databaseService.GetUserByLoginAsync(login);
            var check = VerifyPassword(password, user.PasswordHash);
            if (user != null && check)
            {
                return true;
            }

            return false;
        }

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