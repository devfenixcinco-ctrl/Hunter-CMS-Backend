namespace MreHunter.Api.Models
{
    public class ProductBenefitItem
    {

        
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}