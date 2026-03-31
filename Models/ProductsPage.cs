using Microsoft.AspNetCore.Http;

namespace MreHunter.Api.Models
{
    public class ProductsPage
    {
        public int Id { get; set; }

        public string HeroTitle { get; set; } = string.Empty;
        public string HeroButtonText { get; set; } = string.Empty;
        public string? HeroButtonLink { get; set; }

        public IFormFile? HeroImage { get; set; }
        public string? HeroImageUrl { get; set; }

        public string HelpTitle { get; set; } = string.Empty;
        public string? HelpSubtitle { get; set; }

        public string OfficesTitle { get; set; } = string.Empty;

        public string CoverageTitle { get; set; } = string.Empty;
        public string? CoverageSubtitle { get; set; }
        public string CoverageButtonText { get; set; } = string.Empty;
        public string? CoverageButtonLink { get; set; }

        public IFormFile? CoverageImage { get; set; }
        public string? CoverageImageUrl { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}