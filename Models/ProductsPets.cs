using Microsoft.AspNetCore.Http;

namespace MreHunter.Api.Models
{
    public class ProductsPets
    {
        public int? Id { get; set; }

        public string? HeroTitle { get; set; }
        public string? HeroDescription { get; set; }

        public IFormFile? HeroImage { get; set; }
        public string? HeroImageUrl { get; set; }

        public string? BenefitsTitle { get; set; }

        public string? CardTitle { get; set; }
        public string? CardText { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}