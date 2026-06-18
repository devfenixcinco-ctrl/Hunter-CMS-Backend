using Microsoft.AspNetCore.Http;

namespace MreHunter.Api.Models
{
    public class CompanyPage
    {
        public int? Id { get; set; }

        public string HeroTitle { get; set; } = string.Empty;
        public string? HeroSubtitle { get; set; }

        public IFormFile? HeroImage { get; set; }
        public string? HeroImageUrl { get; set; }

        public string VideoTitle { get; set; } = string.Empty;
        public string? VideoSubtitle { get; set; }
        public string? VideoUrl { get; set; }

        public string CtaTitle { get; set; } = string.Empty;
        public string? CtaSubtitle { get; set; }
        public string CtaButton1Text { get; set; } = string.Empty;
        public string? CtaButton1Link { get; set; }
        public string CtaButton2Text { get; set; } = string.Empty;
        public string? CtaButton2Link { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}