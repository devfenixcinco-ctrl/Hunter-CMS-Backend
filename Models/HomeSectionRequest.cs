using System.ComponentModel.DataAnnotations;

namespace MreHunter.Api.Models
{
    public class HomeSectionRequest
    {
        [Required]
        public string SectionName { get; set; } = string.Empty;

        [Required]
        public string ContentJson { get; set; } = string.Empty;
    }
}