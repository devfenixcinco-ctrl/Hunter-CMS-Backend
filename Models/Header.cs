using System.ComponentModel.DataAnnotations;

namespace MreHunter.Api.Models
{
    public class Header
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public string SupportLine { get; set; } = string.Empty;

        [Required]
        public string TheftNumber { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
