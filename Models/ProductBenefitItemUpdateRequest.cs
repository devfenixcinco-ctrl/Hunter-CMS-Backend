namespace MreHunter.Api.Models
{
    public class ProductBenefitItemUpdateRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }
}