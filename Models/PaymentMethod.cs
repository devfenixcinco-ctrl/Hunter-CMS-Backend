namespace MreHunter.Api.Models
{
    public class PaymentMethod
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public string? ButtonText { get; set; }
        public string? ButtonLink { get; set; }
        public string? Price { get; set; }
        public string? ButtonTextVideo { get; set; }
        public string? ButtonLinkVideo { get; set; }
        public string? ButtonTextWanted { get; set; }
        public string? ButtonLinkWanted { get; set; }
   

        public DateTime CreatedAt { get; set; }
    }
}