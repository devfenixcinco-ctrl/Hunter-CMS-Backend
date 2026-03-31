namespace MreHunter.Api.Models
{
    public class ProductOfficeRequest
    {
        public int ProductsPageId { get; set; }

        public string OfficeName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? MapUrl { get; set; }

        public int SortOrder { get; set; }
    }
}