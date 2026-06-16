using Microsoft.AspNetCore.Http;

namespace MreHunter.Api.Models
{
    public class ProductsDashcamUpdateRequest
    {
        public IFormFile? HeroImage { get; set; }
        public string? HeroImageUrl { get; set; }

        public string? HeroTitle { get; set; }
        public string? HeroHighlightText { get; set; }
        public string? HeroSubtitle { get; set; }

        public string? BenefitsTitle { get; set; }

        public string? ReasonTitle { get; set; }
        public string? ReasonDescription { get; set; }

        public string? PaymentTitle { get; set; }

        public string? PrimaryButtonText { get; set; }
        public string? PrimaryButtonLink { get; set; }
        public string? SecondaryButtonText { get; set; }
        public string? SecondaryButtonLink { get; set; }
    }
}