namespace MreHunter.Api.Models
{
    public class DashcamMediaSectionRequest
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? VideoUrl { get; set; }
    }
}