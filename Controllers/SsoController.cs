using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SmartMeterReadingDash.Models.Dashboard;
using SmartMeterReadingDash.Services;
using System.Security.Cryptography;
using System.Text;

namespace SmartMeterReadingDash.Controllers
{
    [Route("sso")]
    public class SsoController : Controller
    {
        private readonly AuthRepository _authRepository;
        private readonly JwtService _jwtService;
        private readonly SsoSettings _ssoSettings;

        public SsoController( AuthRepository authRepository,  JwtService jwtService, IOptions<SsoSettings> ssoSettings)
        {
            _authRepository = authRepository;
            _jwtService = jwtService;
            _ssoSettings = ssoSettings.Value;
        }

        [HttpGet("login")]
        public async Task<IActionResult> Login( [FromQuery] string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return Unauthorized("SSO token is missing.");
            }

            if (string.IsNullOrWhiteSpace( _ssoSettings.Secret))
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "SSO is not configured.");
            }

            string[] parts = token.Split('.');

            if (parts.Length != 3)
            {
                return Unauthorized("Invalid SSO token.");
            }

            string encodedUsername = parts[0];
            string timestampText = parts[1];
            string receivedSignature = parts[2];

            string username;

            try
            {
                username =  Encoding.UTF8.GetString(FromBase64Url(encodedUsername));
            }
            catch
            {
                return Unauthorized("Invalid SSO username.");
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                return Unauthorized("SSO username is empty.");
            }

            if (!long.TryParse( timestampText,out long timestamp))
            {
                return Unauthorized( "Invalid SSO timestamp.");
            }

            DateTime tokenTime;

            try
            {
                tokenTime =  DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
            }
            catch
            {
                return Unauthorized("Invalid SSO timestamp.");
            }

            TimeSpan age = DateTime.UtcNow - tokenTime;


            if (age.TotalSeconds < -10 ||
                age.TotalSeconds > 60)
            {
                return Unauthorized("SSO token has expired.");
            }

            string data = encodedUsername + "." + timestampText;

            string expectedSignature =
                CreateSignature(
                    data,
                    _ssoSettings.Secret);

            if (!FixedTimeEquals(
                    receivedSignature,
                    expectedSignature))
            {
                return Unauthorized(
                    "Invalid SSO signature.");
            }

            // Find the user in the Smart Meter application's
            // own database.
            var user =
                await _authRepository
                    .GetUserByUsernameAsync(username);

            if (user == null)
            {
                return Unauthorized(
                    "User is not registered in Smart Meter Dashboard.");
            }

            if (user.IsActive != 1)
            {
                return Unauthorized(
                    "User account is inactive.");
            }

            // Generate the normal Smart Meter JWT.
            var jwt =
    _jwtService.GenerateToken(user);

            Response.Cookies.Append(
                "SmartMeterAuth",
                jwt,
                new CookieOptions
                {
                    HttpOnly = true,

                    Secure = Request.IsHttps,

                    SameSite = Request.IsHttps
                        ? SameSiteMode.None
                        : SameSiteMode.Lax,

                    Expires =
                        DateTimeOffset.UtcNow.AddMinutes(60),

                    Path = "/"
                });

            return Redirect("/");

        }

        private static string CreateSignature(
            string data,
            string secret)
        {
            using var hmac =
                new HMACSHA256(
                    Encoding.UTF8.GetBytes(secret));

            byte[] hash =
                hmac.ComputeHash(
                    Encoding.UTF8.GetBytes(data));

            return ToBase64Url(hash);
        }

        private static bool FixedTimeEquals(
            string a,
            string b)
        {
            if (string.IsNullOrEmpty(a) ||
                string.IsNullOrEmpty(b))
            {
                return false;
            }

            byte[] aBytes =
                Encoding.UTF8.GetBytes(a);

            byte[] bBytes =
                Encoding.UTF8.GetBytes(b);

            if (aBytes.Length != bBytes.Length)
            {
                return false;
            }

            int result = 0;

            for (int i = 0; i < aBytes.Length; i++)
            {
                result |=
                    aBytes[i] ^ bBytes[i];
            }

            return result == 0;
        }

        private static string ToBase64Url(
            byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static byte[] FromBase64Url(
            string value)
        {
            string s =
                value
                    .Replace('-', '+')
                    .Replace('_', '/');

            switch (s.Length % 4)
            {
                case 2:
                    s += "==";
                    break;

                case 3:
                    s += "=";
                    break;

                case 1:
                    throw new FormatException(
                        "Invalid Base64Url string.");
            }

            return Convert.FromBase64String(s);
        }
    }
}