namespace MreHunter.Api.Models
{
    public class HomeUpdateRequest
    {
        public string? BannerTitle { get; set; }
        public string? BannerText { get; set; }

        public string? FirstCardTitle { get; set; }
        public string? FirstCardText { get; set; }

        public string? NumbersTitle { get; set; }

        public string? VideoUrl { get; set; }

        public int? YearsExperience { get; set; }
        public int? ActiveClients { get; set; }
        public int? Recoveries { get; set; }
        public int? Captures { get; set; }

        public List<string>? Countries { get; set; }

        public string? SecondBanner { get; set; }
        public string? SecondBannerText { get; set; }
        public string? SecondBannerNumber { get; set; }

        public string? ThirdBanner { get; set; }
        public string? ThirdBannerText { get; set; }
        public string? ThirdBannerHighlight { get; set; }
        public string? ThirdBannerEmphasis { get; set; }

        public string? FourthBanner { get; set; }

      
        public string? FourthBannerTitle { get; set; }
        public string? FourthBannerText { get; set; }

        public string? BannerButtonURL { get; set; }
    }
}