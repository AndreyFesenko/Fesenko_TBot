using System.ComponentModel.DataAnnotations;


namespace Fesenko_TBot.Models
{
    public record ATM
    {
        [Key]
        public string IdATM { get; set; }
        public string? Model { get; set; }
        public string? Address { get; set; }
        public string? Client { get; set; }
        public string? City { get; set; }
        public string? Coordinates { get; set; }
    }
}
