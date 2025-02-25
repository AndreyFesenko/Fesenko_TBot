using System.ComponentModel.DataAnnotations;

namespace Fesenko_TBot.Models
{
    public record User
    {
        [Key]
        public string Login { get; set; }
        public string PasswordHash { get; set; }
    }
}
