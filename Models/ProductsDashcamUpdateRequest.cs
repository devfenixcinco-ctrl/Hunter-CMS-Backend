using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MreHunter.Api.Models
{
    public class ProductsDashcamUpdateRequest
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
        public string? BenefitsTitle { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        public string? BenefitsJson { get; set; }

        [Required]
        public string? ReasonTitle { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        public string? ReasonDescription { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        public string? MediaSectionsJson { get; set; }

        [Required]
        public string? PaymentTitle { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        public string? PaymentMethodsJson { get; set; }

        public string? PrimaryButtonText { get; set; }
        public string? PrimaryButtonLink { get; set; }
        public string? SecondaryButtonText { get; set; }
        public string? SecondaryButtonLink { get; set; }
    }
}