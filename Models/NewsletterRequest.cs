using System.ComponentModel.DataAnnotations;

namespace MreHunter.Api.Models
{
    public class NewsletterRequest
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }
    }
}