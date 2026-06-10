using System.ComponentModel.DataAnnotations;

namespace MreHunter.Api.Models
{
    public class PaymentMethodRequest
    {
        [MaxLength(200)]
        public string? Title { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? Icon { get; set; }

        [MaxLength(100)]
        public string? ButtonText { get; set; }

        [MaxLength(100)]
        public string? Price { get; set; }

        [MaxLength(100)]
        public string? ButtonTextVideo { get; set; }

        [MaxLength(2000)]
        public string? ButtonLinkVideo { get; set; }

        [MaxLength(100)]
        public string? ButtonTextWanted { get; set; }

        [MaxLength(2000)]
        public string? ButtonLinkWanted { get; set; }

        [MaxLength(2000)]
        public string? ButtonLink { get; set; }
    }
}