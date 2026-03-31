using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MreHunter.Api.Models
{
    public class JobApplicationRequest
    {
        [Required]
        [StringLength(50)]
        public string DocumentId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public IFormFile CvFile { get; set; } = default!;
    }
}