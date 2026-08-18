namespace SmartMeterReadingDash.Models.Dashboard
{
    public class DashboardUser
    {
        public int UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string? FullName { get; set; }

        public string Role { get; set; } = "USER";

        public int IsActive { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? LastLoginDate { get; set; }
    }
}
