using System.ComponentModel.DataAnnotations;


namespace Fesenko_TBot.Models
{
    public record Incident
    {
        [Key]
        public int IdInc { get; set; }
        public string? Description { get; set; }
        public string? Service { get; set; }
        public string? Status { get; set; }
        public string? City { get; set; }
        public string? ATM { get; set; }
        public int? IdEng { get; set; }
        public DateTime Date { get; set; }
        public DateTime Deadline { get; set; }
    }
}
