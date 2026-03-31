using System.ComponentModel.DataAnnotations;

namespace MreHunter.Api.Models
{
    public class ContactInfoRequest
    {
        [Required]
        public string? Phone { get; set; }

        public string? WhatsappText { get; set; }

        public string? WhatsappLink { get; set; }

        [EmailAddress]
        public string? Email { get; set; }
    }
}