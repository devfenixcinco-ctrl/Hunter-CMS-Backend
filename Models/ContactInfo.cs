namespace MreHunter.Api.Models
{
    public class ContactInfo
    {
        public int Id { get; set; }
        public string? Phone { get; set; }
        public string? WhatsappText { get; set; }
        public string? WhatsappLink { get; set; }
        public string? Email { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}