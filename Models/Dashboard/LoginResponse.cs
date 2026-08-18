namespace SmartMeterReadingDash.Models.Dashboard
{
    public class LoginResponse
    {
        public bool Success { get; set; }

        public string Token { get; set; } = string.Empty;

        public int ExpiresIn { get; set; }

        public string Username { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
    }
}
