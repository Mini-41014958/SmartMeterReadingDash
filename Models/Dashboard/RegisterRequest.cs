namespace SmartMeterReadingDash.Models.Dashboard
{
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "USER";
        public string? Company { get; set; }
        public string? Department { get; set; }

    }
}
