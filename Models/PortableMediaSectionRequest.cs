using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MreHunter.Api.Models
{
    public class PortableMediaSectionRequest
    {
        [Required]
        public string? Title { get; set; }

        public string? Description { get; set; }

        public string? VideoUrl { get; set; }

    }
}