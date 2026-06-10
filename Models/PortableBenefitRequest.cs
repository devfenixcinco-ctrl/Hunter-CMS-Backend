using System.ComponentModel.DataAnnotations;

namespace MreHunter.Api.Models
{
    public class PortableBenefitRequest
    {
        [Required]
        public string? Title { get; set; }

        [Required]
        public string? Icon { get; set; }

     
    }
}