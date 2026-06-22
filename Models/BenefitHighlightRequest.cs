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

        [MaxLength(255)]
        public string? BenefitsTitle { get; set; }
    }
}