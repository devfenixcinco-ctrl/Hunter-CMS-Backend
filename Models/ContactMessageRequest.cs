using System.ComponentModel.DataAnnotations;

namespace MreHunter.Api.Models
{
    public class ContactMessageRequest
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;

        public string? LastName { get; set; }

        public string? Phone { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;
    }
}