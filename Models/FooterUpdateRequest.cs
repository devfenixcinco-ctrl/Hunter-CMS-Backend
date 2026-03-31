using System.ComponentModel.DataAnnotations;

namespace MreHunter.Api.Models
{
    public class FooterUpdateRequest
    {
        [Required]
        public string? FooterText { get; set; }

        [Required]
        public string? Address { get; set; }

        [Required]
        public string? Email { get; set; }

        [Required]
        public string? SocialUsername { get; set; }
    }
}