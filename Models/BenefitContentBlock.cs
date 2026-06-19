namespace MreHunter.Api.Models
{
    public class BenefitContentBlock
    {
        public int Id { get; set; }

        public string? Title { get; set; }
        public string? Description { get; set; }

        public string? MediaUrl { get; set; }
        public string? MediaType { get; set; }

        public string? TextPosition { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}