using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartMeterReadingDash.Models.Dashboard;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SmartMeterReadingDash.Services
{
    public class JwtService
    {
        private readonly JWTSettings _jwtSettings;

        public JwtService(
            IOptions<JWTSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }

        public string GenerateToken(DashboardUser user)
        {
         var claims = new List<Claim>
         {
         new Claim( ClaimTypes.NameIdentifier, user.UserId.ToString()),

         new Claim( ClaimTypes.Name, user.Username),

         new Claim( ClaimTypes.Role, user.Role.ToUpperInvariant())
        };

            if (!string.IsNullOrWhiteSpace(user.FullName))
            {
                claims.Add( new Claim( "fullName",  user.FullName));
            }

            if (!string.IsNullOrWhiteSpace(user.Company))
            {
                claims.Add( new Claim( "company", user.Company.Trim().ToUpperInvariant()));
            }

            if (!string.IsNullOrWhiteSpace(user.Department))
            {
                claims.Add( new Claim( "department",user.Department.Trim().ToUpperInvariant()));
            }

            var key = new SymmetricSecurityKey( Encoding.UTF8.GetBytes(_jwtSettings.Key) );

            var credentials = new SigningCredentials( key,  SecurityAlgorithms.HmacSha256);

            var expires =  DateTime.UtcNow.AddMinutes( _jwtSettings.ExpiryMinutes );

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
