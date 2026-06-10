namespace MreHunter.Api.Models
{
    public class GpsSection
    {
        public int Id { get; set; }

        public string? WhyTitle { get; set; }
        public string? WhyDescription { get; set; }

        public string? VideoTitle1 { get; set; }
        public string? VideoDescription1 { get; set; }
        public string? VideoUrl1 { get; set; }
        public string? VideoImage1 { get; set; }

        public string? VideoTitle2 { get; set; }
        public string? VideoDescription2 { get; set; }
        public string? VideoUrl2 { get; set; }
        public string? VideoImage2 { get; set; }

        public string? ProductTitle { get; set; }
        public string? ProductDescription { get; set; }
        public string? ProductPrice { get; set; }
        public string? ProductIcon { get; set; }

        public string? ButtonText { get; set; }
        public string? ButtonUrl { get; set; }
    }
}