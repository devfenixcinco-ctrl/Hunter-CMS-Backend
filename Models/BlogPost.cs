namespace MreHunter.Api.Models
{
    public class BlogPost
    {
        public int Id { get; set; }

        public string? ImageUrl { get; set; }
        public string? Title { get; set; }

        // En tu controller lo llamas Text pero viene de la columna "content"
        public string? Text { get; set; }

        public DateTime? PublishDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}