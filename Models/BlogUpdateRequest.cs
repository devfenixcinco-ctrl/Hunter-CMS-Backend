namespace MreHunter.Api.Models
{
    public class BlogUpdateRequest
    {
        public IFormFile? Image { get; set; }
        public string? Title { get; set; }
        public string? Text { get; set; }
        public DateTime? PublishDate { get; set; }
    }
}