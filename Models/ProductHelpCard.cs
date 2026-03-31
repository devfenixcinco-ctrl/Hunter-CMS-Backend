namespace MreHunter.Api.Models
{
    public class ProductHelpCard
    {
        public int Id { get; set; }
        public int ProductsPageId { get; set; }

        public string? Icon { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string ButtonText { get; set; } = string.Empty;
        public string? ButtonLink { get; set; }

        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}