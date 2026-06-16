namespace MreHunter.Api.Models
{
    public class ProductsPetsMediaSection
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? VideoUrl { get; set; }
        public int? SortOrder { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}