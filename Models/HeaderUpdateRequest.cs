using System.ComponentModel.DataAnnotations;

namespace MreHunter.Api.Models
{
    public class HeaderUpdateRequest
    {

        [Required]
        public string? SupportLine { get; set; }

        [Required]
        public string? TheftNumber { get; set; }

        public string? Lunes { get; set; }

        public string? Martes { get; set; }

        public string? Miercoles { get; set; }

        public string? Jueves { get; set; }

        public string? Viernes { get; set; }
        
        
    }
}
