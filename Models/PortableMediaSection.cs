using Microsoft.AspNetCore.Http;

namespace MreHunter.Api.Models
{
    public class PortableMediaSection
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? VideoUrl { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}