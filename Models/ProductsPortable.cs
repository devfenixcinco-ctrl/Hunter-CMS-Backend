using Microsoft.AspNetCore.Http;

namespace MreHunter.Api.Models
{
    public class ProductsPortable
    {
        public int Id { get; set; }

        public string HeroTitle { get; set; } = string.Empty;
        public string? HeroSubtitle { get; set; }
        public string? HeroHighlightText { get; set; }

        public IFormFile? HeroImage { get; set; }
        public string? HeroImageUrl { get; set; }

        public string BenefitsTitle { get; set; } = string.Empty;
        public string? BenefitsJson { get; set; }

        public string ReasonTitle { get; set; } = string.Empty;
        public string? ReasonDescription { get; set; }

        public string? MediaSectionsJson { get; set; }

        public string PaymentTitle { get; set; } = string.Empty;
        public string? PrimaryButtonText { get; set; }
        public string? PrimaryButtonLink { get; set; }
        public string? SecondaryButtonText { get; set; }
        public string? SecondaryButtonLink { get; set; }
        public string? PaymentMethodsJson { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}