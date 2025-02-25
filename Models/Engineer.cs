using System.ComponentModel.DataAnnotations;


namespace Fesenko_TBot.Models
{

    public record Engineer
    {
        [Key]
        public int IdEng { get; set; }
        public string NameEng { get; set; }
        public string? Status { get; set; }
        public string? Coordinates { get; set; }
        public string? City { get; set; }
    }
}
