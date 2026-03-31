using System.ComponentModel.DataAnnotations;

namespace MreHunter.Api.Models
{
    public class BenefitHighlightRequest
    {
        [Required]
        [MaxLength(200)]
        public string? Title { get; set; }

        [Required]
        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(50)]
        public string? BackgroundColor { get; set; }

        [Required]
        [MaxLength(50)]
        public string? TextColor { get; set; }
    }
}