using System.ComponentModel.DataAnnotations;

namespace Fesenko_TBot.Models
{
    public class User
    {
        [Key]
        public string Login { get; set; }
        public string PasswordHash { get; set; }
    }
}
