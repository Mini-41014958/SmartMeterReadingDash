using SmartMeterReadingDash.Models.Dashboard;
using System.Security.Claims;

namespace SmartMeterReadingDash.Services
{
    public class UserAccess
    {
        public UserAccessScope GetScope(ClaimsPrincipal user)
        {
            if (user?.Identity == null ||
                !user.Identity.IsAuthenticated)
            {
                throw new UnauthorizedAccessException(
                    "User is not authenticated."
                );
            }

            var userIdValue =
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdValue, out var userId))
            {
                throw new UnauthorizedAccessException(
                    "Invalid user identity."
                );
            }

            var username =
                user.FindFirst(ClaimTypes.Name)?.Value
                ?? string.Empty;

            var role =
                user.FindFirst(ClaimTypes.Role)?.Value
                ?? string.Empty;

            var company =
                user.FindFirst("company")?.Value;

            var department =
                user.FindFirst("department")?.Value;

            return new UserAccessScope
            {
                UserId = userId,

                Username = username,

                Role = role.ToUpperInvariant(),

                Company =
                    string.IsNullOrWhiteSpace(company)
                        ? null
                        : company.Trim().ToUpperInvariant(),

                Department =
                    string.IsNullOrWhiteSpace(department)
                        ? null
                        : department.Trim().ToUpperInvariant()
            };
        }
    }
}