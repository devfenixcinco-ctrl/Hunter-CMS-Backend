namespace MreHunter.Api.Models
{
    public class FooterInfo
    {
        public int Id { get; set; }
        public string? FooterText { get; set; }
        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? SocialUsername { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
