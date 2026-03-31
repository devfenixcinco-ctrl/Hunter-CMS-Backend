namespace MreHunter.Api.Models
{
    public class GeneralSettings
    {
        public int Id { get; set; }
        public string Logo { get; set; } = string.Empty;
        public string PrimaryColor { get; set; } = string.Empty;
        public string WhatsappNumber { get; set; } = string.Empty;

        public string? SecondaryColor { get; set; }  = string.Empty;

        public string? TextPrimaryColor { get; set; }  = string.Empty;

        public string? TextSecondaryColor { get; set; }  = string.Empty;

        public string? HoverBackgroundColor { get; set; }  = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
