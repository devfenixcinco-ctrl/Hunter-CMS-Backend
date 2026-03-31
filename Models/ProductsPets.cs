using Microsoft.AspNetCore.Http;

namespace MreHunter.Api.Models
{
    public class ProductsPets
    {
        public int Id { get; set; }

        public string HeroTitle { get; set; } = string.Empty;
        public string? HeroDescription { get; set; }

        public IFormFile? HeroImage { get; set; }
        public string? HeroImageUrl { get; set; }

        public string BenefitsTitle { get; set; } = string.Empty;
        public List<ProductsPetsBenefit> Benefits { get; set; } = new();

        public string CreditTitle { get; set; } = string.Empty;
        public string? CreditSubtitle { get; set; }
        public string? CreditLegalText { get; set; }
        public string? CreditButtonText { get; set; }
        public string? CreditButtonLink { get; set; }
        public List<ProductsPetsCreditFeature> CreditFeatures { get; set; } = new();

        public List<ProductsPetsMediaSection> MediaSections { get; set; } = new();

        public string PaymentTitle { get; set; } = string.Empty;
        public string? PrimaryButtonText { get; set; }
        public string? PrimaryButtonLink { get; set; }
        public string? SecondaryButtonText { get; set; }
        public string? SecondaryButtonLink { get; set; }
        public List<ProductsPetsPaymentMethod> PaymentMethods { get; set; } = new();

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ProductsPetsBenefit
    {
        public string? Icon { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public int SortOrder { get; set; }
    }

    public class ProductsPetsCreditFeature
    {
        public string? Icon { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }

    public class ProductsPetsMediaSection
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? MediaType { get; set; }
        public string? MediaUrl { get; set; }
        public string? MediaPosition { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ProductsPetsPaymentMethod
    {
        public string? Icon { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ButtonText { get; set; }
        public string? ButtonLink { get; set; }
        public int SortOrder { get; set; }
    }
}