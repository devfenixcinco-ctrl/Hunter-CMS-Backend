namespace MreHunter.Api.Models
{
    public class BenefitHighlight
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}