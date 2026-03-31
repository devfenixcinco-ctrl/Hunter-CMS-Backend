using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MreHunter.Api.Models
{
    public class FaqSettingsUpdateRequest
    {
        public IFormFile? HeroImage { get; set; }

        public string? HeroImageUrl { get; set; }

        [Required]
        public string? HeroTitle { get; set; }

        [Required]
        public string? HeroHighlightText { get; set; }

        [Required]
        public string? HeroSubtitle { get; set; }

        [Required]
        public string? Question1 { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        public string? Answer1 { get; set; }

        [Required]
        public string? Question2 { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        public string? Answer2 { get; set; }

        [Required]
        public string? Question3 { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        public string? Answer3 { get; set; }

        [Required]
        public string? Question4 { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        public string? Answer4 { get; set; }

        [Required]
        public string? Question5 { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        public string? Answer5 { get; set; }

        [Required]
        public string? Question6 { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        public string? Answer6 { get; set; }
    }
}