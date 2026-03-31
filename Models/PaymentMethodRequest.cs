using System.ComponentModel.DataAnnotations;

namespace MreHunter.Api.Models
{
    public class PaymentMethodRequest
    {
        [Required]
        [MaxLength(200)]
        public string? Title { get; set; }

        [Required]
        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(100)]
        public string? Icon { get; set; }

        [Required]
        [MaxLength(100)]
        public string? ButtonText { get; set; }

        [Required]
        [MaxLength(500)]
        public string? ButtonLink { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "DisplayOrder debe ser mayor a 0.")]
        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}