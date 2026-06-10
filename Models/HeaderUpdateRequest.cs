using System.ComponentModel.DataAnnotations;

namespace MreHunter.Api.Models
{
    public class HeaderUpdateRequest
    {

        [Required]
        public string? SupportLine { get; set; }

        [Required]
        public string? TheftNumber { get; set; }
    }
}
