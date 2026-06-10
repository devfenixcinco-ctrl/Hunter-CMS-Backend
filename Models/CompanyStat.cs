namespace MreHunter.Api.Models
{
    public class CompanyStat
    {
        public int Id { get; set; }
        public int CompanyPageId { get; set; }

        public string UsersCount { get; set; } = string.Empty;
        public string UsersLabel { get; set; } = string.Empty;

        public string RecoveriesCount { get; set; } = string.Empty;
        public string RecoveriesLabel { get; set; } = string.Empty;

        public string CapturesCount { get; set; } = string.Empty;
        public string CapturesLabel { get; set; } = string.Empty;

        public DateTime? CreatedAt { get; set; }
    }
}