using System.ComponentModel.DataAnnotations;

namespace MreHunter.Api.Models
{
    public class BlogSettingsUpdateRequest
    {
        [Required]
        public string? BannerTitle { get; set; }

        [Required]
        public string? BannerSubtitle { get; set; }

        [Required]
        public string? SubscriptionTitle { get; set; }

        [Required]
        public string? SubscriptionSubtitle { get; set; }

        [Required]
        public string? AcceptanceText { get; set; }
    }
}