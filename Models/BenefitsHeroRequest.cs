using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MreHunter.Api.Models
{
    public class BenefitsHeroRequest
    {
        [Required]
        [MaxLength(200)]
        public string? Title { get; set; }

        [Required]
        [MaxLength(300)]
        public string? Subtitle { get; set; }

        [Required]
        [MaxLength(500)]
        public string? HighlightText { get; set; }

        public IFormFile? Image { get; set; }

        public bool IsActive { get; set; }
    }
}