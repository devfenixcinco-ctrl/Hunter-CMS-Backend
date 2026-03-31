namespace MreHunter.Api.Models
{
    public class BenefitContentBlockRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? MediaUrl { get; set; }
        public string MediaType { get; set; } = string.Empty;
        public string TextPosition { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }
}