using System.ComponentModel.DataAnnotations;

namespace MreHunter.Api.Models
{
    public class Header
    {

        public int Id { get; set; }

        [Required]
        public string SupportLine { get; set; } = string.Empty;

        [Required]
        public string TheftNumber { get; set; } = string.Empty;

        public string? Lunes { get; set; }
        public string? Martes { get; set; }
        public string? Miercoles { get; set; }
        public string? Jueves { get; set; }
        public string? Viernes { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }
}
