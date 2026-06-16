namespace MreHunter.Api.Models
{
    public class PlanPlusSection
    {
        public int Id { get; set; }

        public string? HeroBadge { get; set; }
        public string? HeroText { get; set; }
        public string? HeroLeftImage { get; set; }

        public string? BenefitsTitle { get; set; }

        public string? QuoteTitle { get; set; }
        public string? QuoteSubtitle { get; set; }
        public string? QuoteCardTitle { get; set; }
        public string? QuoteNote { get; set; }
        public string? QuoteButtonText { get; set; }

        public string? VideoTitle1 { get; set; }
        public string? VideoDescription1 { get; set; }
        public string? VideoUrl1 { get; set; }

        public string? VideoTitle2 { get; set; }
        public string? VideoDescription2 { get; set; }
        public string? VideoUrl2 { get; set; }
    }
}